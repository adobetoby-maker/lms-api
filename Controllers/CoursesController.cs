using System.Security.Claims;
using lms_api.Data;
using lms_api.DTOs.Courses;
using lms_api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lms_api.Controllers;

[ApiController]
[Route("api/courses")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<CoursesController> _logger;

    public CoursesController(AppDbContext db, ILogger<CoursesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();

    /// <summary>Get all courses the current learner is enrolled in.</summary>
    [HttpGet]
    public async Task<ActionResult<List<CourseListDto>>> GetMyCourses()
    {
        string userId = GetUserId();

        List<Enrollment> enrollments = await _db.Enrollments
            .Include(e => e.Course)
                .ThenInclude(c => c.Videos)
            .Include(e => e.Course)
                .ThenInclude(c => c.Questions)
            .Where(e => e.UserId == userId)
            .ToListAsync();

        List<CourseListDto> result = enrollments.Select(e => new CourseListDto
        {
            Id = e.Course.Id,
            Title = e.Course.Title,
            Description = e.Course.Description,
            RequireFullVideoWatch = e.Course.RequireFullVideoWatch,
            IsActive = e.Course.IsActive,
            CreatedAt = e.Course.CreatedAt,
            VideoCount = e.Course.Videos.Count,
            QuestionCount = e.Course.Questions.Count,
            EnrollmentStatus = e.Status.ToString(),
            VideoWatched = e.VideoWatched,
            VideoProgressSeconds = e.VideoProgressSeconds
        }).ToList();

        return Ok(result);
    }

    /// <summary>Get a single course with videos (learner must be enrolled).</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CourseDto>> GetCourse(int id)
    {
        string userId = GetUserId();

        bool isAdmin = User.IsInRole("Admin");

        Enrollment? enrollment = null;
        if (!isAdmin)
        {
            enrollment = await _db.Enrollments
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == id);

            if (enrollment == null)
                return Forbid();
        }

        Course? course = await _db.Courses
            .Include(c => c.Videos.OrderBy(v => v.SortOrder))
            .Include(c => c.Questions)
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

        if (course == null)
            return NotFound(new { message = "Course not found." });

        CourseDto dto = new CourseDto
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            RequireFullVideoWatch = course.RequireFullVideoWatch,
            IsActive = course.IsActive,
            CreatedAt = course.CreatedAt,
            QuestionCount = course.Questions.Count,
            Videos = course.Videos.Select(v => new VideoDto
            {
                Id = v.Id,
                Title = v.Title,
                Type = v.Type.ToString(),
                Url = v.Url,
                DurationSeconds = v.DurationSeconds,
                SortOrder = v.SortOrder
            }).ToList()
        };

        return Ok(dto);
    }

    /// <summary>Update video progress for the current learner.</summary>
    [HttpPost("{id:int}/progress")]
    public async Task<IActionResult> UpdateProgress(int id, [FromBody] VideoProgressRequest request)
    {
        string userId = GetUserId();

        Enrollment? enrollment = await _db.Enrollments
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == id);

        if (enrollment == null)
            return NotFound(new { message = "Enrollment not found." });

        enrollment.VideoProgressSeconds = request.ProgressSeconds;

        if (request.MarkWatched)
        {
            enrollment.VideoWatched = true;
        }

        // Auto-advance status from Invited to InProgress when progress is made
        if (enrollment.Status == EnrollmentStatus.Invited && request.ProgressSeconds > 0)
        {
            enrollment.Status = EnrollmentStatus.InProgress;
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Progress saved.",
            videoWatched = enrollment.VideoWatched,
            progressSeconds = enrollment.VideoProgressSeconds,
            status = enrollment.Status.ToString()
        });
    }
}
