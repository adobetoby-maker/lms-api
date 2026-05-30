using lms_api.Data;
using lms_api.DTOs.Admin;
using lms_api.DTOs.Courses;
using lms_api.Models;
using lms_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lms_api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ILogger<AdminController> logger)
    {
        _db = db;
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    // ─── Courses ────────────────────────────────────────────────────────────

    /// <summary>List all courses.</summary>
    [HttpGet("courses")]
    public async Task<ActionResult<List<CourseListDto>>> GetAllCourses()
    {
        List<Course> courses = await _db.Courses
            .Include(c => c.Videos)
            .Include(c => c.Questions)
            .Include(c => c.Enrollments)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        List<CourseListDto> result = courses.Select(c => new CourseListDto
        {
            Id = c.Id,
            Title = c.Title,
            Description = c.Description,
            RequireFullVideoWatch = c.RequireFullVideoWatch,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt,
            VideoCount = c.Videos.Count,
            QuestionCount = c.Questions.Count,
            EnrollmentStatus = string.Empty,
            VideoWatched = false,
            VideoProgressSeconds = 0
        }).ToList();

        return Ok(result);
    }

    /// <summary>Create a new course.</summary>
    [HttpPost("courses")]
    public async Task<ActionResult<CourseDto>> CreateCourse([FromBody] CreateCourseRequest request)
    {
        Course course = new Course
        {
            Title = request.Title,
            Description = request.Description,
            RequireFullVideoWatch = request.RequireFullVideoWatch,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.Courses.Add(course);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAllCourses), new { id = course.Id }, new CourseDto
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            RequireFullVideoWatch = course.RequireFullVideoWatch,
            IsActive = course.IsActive,
            CreatedAt = course.CreatedAt,
            Videos = new List<VideoDto>(),
            QuestionCount = 0
        });
    }

    /// <summary>Update a course (including RequireFullVideoWatch toggle).</summary>
    [HttpPut("courses/{id:int}")]
    public async Task<ActionResult<CourseDto>> UpdateCourse(int id, [FromBody] UpdateCourseRequest request)
    {
        Course? course = await _db.Courses
            .Include(c => c.Videos)
            .Include(c => c.Questions)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null)
            return NotFound(new { message = "Course not found." });

        if (request.Title != null) course.Title = request.Title;
        if (request.Description != null) course.Description = request.Description;
        if (request.RequireFullVideoWatch.HasValue) course.RequireFullVideoWatch = request.RequireFullVideoWatch.Value;
        if (request.IsActive.HasValue) course.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync();

        return Ok(new CourseDto
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
        });
    }

    /// <summary>Delete a course.</summary>
    [HttpDelete("courses/{id:int}")]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        Course? course = await _db.Courses.FindAsync(id);
        if (course == null)
            return NotFound(new { message = "Course not found." });

        _db.Courses.Remove(course);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ─── Videos ─────────────────────────────────────────────────────────────

    /// <summary>Add a video to a course.</summary>
    [HttpPost("courses/{id:int}/videos")]
    public async Task<ActionResult<VideoDto>> AddVideo(int id, [FromBody] CreateVideoRequest request)
    {
        Course? course = await _db.Courses.FindAsync(id);
        if (course == null)
            return NotFound(new { message = "Course not found." });

        Video video = new Video
        {
            CourseId = id,
            Title = request.Title,
            Type = request.Type,
            Url = request.Url,
            DurationSeconds = request.DurationSeconds,
            SortOrder = request.SortOrder
        };

        _db.Videos.Add(video);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(AddVideo), new { id = video.Id }, new VideoDto
        {
            Id = video.Id,
            Title = video.Title,
            Type = video.Type.ToString(),
            Url = video.Url,
            DurationSeconds = video.DurationSeconds,
            SortOrder = video.SortOrder
        });
    }

    // ─── Questions ───────────────────────────────────────────────────────────

    /// <summary>Add a question to a course.</summary>
    [HttpPost("courses/{id:int}/questions")]
    public async Task<IActionResult> AddQuestion(int id, [FromBody] CreateQuestionRequest request)
    {
        Course? course = await _db.Courses.FindAsync(id);
        if (course == null)
            return NotFound(new { message = "Course not found." });

        Question question = new Question
        {
            CourseId = id,
            Text = request.Text,
            OptionA = request.OptionA,
            OptionB = request.OptionB,
            OptionC = request.OptionC,
            OptionD = request.OptionD,
            CorrectAnswer = request.CorrectAnswer.ToUpper(),
            SortOrder = request.SortOrder
        };

        _db.Questions.Add(question);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(AddQuestion), new { id = question.Id }, new
        {
            question.Id,
            question.CourseId,
            question.Text,
            question.OptionA,
            question.OptionB,
            question.OptionC,
            question.OptionD,
            question.CorrectAnswer,
            question.SortOrder
        });
    }

    /// <summary>Update a question.</summary>
    [HttpPut("questions/{id:int}")]
    public async Task<IActionResult> UpdateQuestion(int id, [FromBody] CreateQuestionRequest request)
    {
        Question? question = await _db.Questions.FindAsync(id);
        if (question == null)
            return NotFound(new { message = "Question not found." });

        question.Text = request.Text;
        question.OptionA = request.OptionA;
        question.OptionB = request.OptionB;
        question.OptionC = request.OptionC;
        question.OptionD = request.OptionD;
        question.CorrectAnswer = request.CorrectAnswer.ToUpper();
        question.SortOrder = request.SortOrder;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            question.Id,
            question.CourseId,
            question.Text,
            question.OptionA,
            question.OptionB,
            question.OptionC,
            question.OptionD,
            question.CorrectAnswer,
            question.SortOrder
        });
    }

    /// <summary>Delete a question.</summary>
    [HttpDelete("questions/{id:int}")]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
        Question? question = await _db.Questions.FindAsync(id);
        if (question == null)
            return NotFound(new { message = "Question not found." });

        _db.Questions.Remove(question);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ─── Invitations ─────────────────────────────────────────────────────────

    /// <summary>Send an invitation email and create an Invite record.</summary>
    [HttpPost("courses/{id:int}/invite")]
    public async Task<IActionResult> InviteUser(int id, [FromBody] InviteRequest request)
    {
        Course? course = await _db.Courses.FindAsync(id);
        if (course == null)
            return NotFound(new { message = "Course not found." });

        Invite invite = new Invite
        {
            Email = request.Email,
            CourseId = id,
            Token = Guid.NewGuid().ToString("N"),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _db.Invites.Add(invite);
        await _db.SaveChangesAsync();

        await _emailService.SendInviteEmailAsync(request.Email, course.Title, invite.Token);

        return Ok(new
        {
            message = $"Invitation sent to {request.Email}.",
            token = invite.Token,
            expiresAt = invite.ExpiresAt
        });
    }

    // ─── Enrollment Reports ──────────────────────────────────────────────────

    /// <summary>List all enrollments for a specific course.</summary>
    [HttpGet("courses/{id:int}/enrollments")]
    public async Task<ActionResult<List<EnrollmentSummaryDto>>> GetCourseEnrollments(int id)
    {
        List<Enrollment> enrollments = await _db.Enrollments
            .Include(e => e.User)
            .Include(e => e.Course)
            .Include(e => e.QuizAttempts)
            .Where(e => e.CourseId == id)
            .OrderBy(e => e.InvitedAt)
            .ToListAsync();

        List<EnrollmentSummaryDto> result = enrollments.Select(e => new EnrollmentSummaryDto
        {
            EnrollmentId = e.Id,
            UserId = e.UserId,
            UserEmail = e.User.Email ?? string.Empty,
            UserFirstName = e.User.FirstName,
            UserLastName = e.User.LastName,
            CourseId = e.CourseId,
            CourseTitle = e.Course.Title,
            Status = e.Status.ToString(),
            VideoWatched = e.VideoWatched,
            VideoProgressSeconds = e.VideoProgressSeconds,
            InvitedAt = e.InvitedAt,
            CompletedAt = e.CompletedAt,
            BestScore = e.QuizAttempts.Any() ? e.QuizAttempts.Max(a => a.Score) : 0,
            AttemptCount = e.QuizAttempts.Count
        }).ToList();

        return Ok(result);
    }

    /// <summary>Get all completions across all courses.</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        int totalCourses = await _db.Courses.CountAsync(c => c.IsActive);
        int totalEnrollments = await _db.Enrollments.CountAsync();
        int totalCompletions = await _db.Enrollments.CountAsync(e => e.Status == EnrollmentStatus.Passed);
        int activeLearnersCount = await _db.Enrollments
            .Where(e => e.Status == EnrollmentStatus.InProgress)
            .Select(e => e.UserId).Distinct().CountAsync();

        // Per-course breakdown
        var courses = await _db.Courses
            .Where(c => c.IsActive)
            .Select(c => new
            {
                c.Id, c.Title,
                Enrolled   = c.Enrollments.Count,
                Completed  = c.Enrollments.Count(e => e.Status == EnrollmentStatus.Passed),
                InProgress = c.Enrollments.Count(e => e.Status == EnrollmentStatus.InProgress),
            }).ToListAsync();

        // Recent activity (last 10 enrollments)
        var recent = await _db.Enrollments
            .Include(e => e.User)
            .Include(e => e.Course)
            .OrderByDescending(e => e.InvitedAt)
            .Take(10)
            .Select(e => new {
                e.Id, e.Status,
                UserEmail = e.User.Email,
                UserName = e.User.FirstName + " " + e.User.LastName,
                CourseTitle = e.Course.Title,
                e.InvitedAt, e.CompletedAt
            }).ToListAsync();

        return Ok(new { totalCourses, totalEnrollments, totalCompletions, activeLearnersCount, courses, recent });
    }

    [HttpGet("completions")]
    public async Task<ActionResult<List<EnrollmentSummaryDto>>> GetAllCompletions()
    {
        List<Enrollment> completions = await _db.Enrollments
            .Include(e => e.User)
            .Include(e => e.Course)
            .Include(e => e.QuizAttempts)
            .Where(e => e.Status == EnrollmentStatus.Passed)
            .OrderByDescending(e => e.CompletedAt)
            .ToListAsync();

        List<EnrollmentSummaryDto> result = completions.Select(e => new EnrollmentSummaryDto
        {
            EnrollmentId = e.Id,
            UserId = e.UserId,
            UserEmail = e.User.Email ?? string.Empty,
            UserFirstName = e.User.FirstName,
            UserLastName = e.User.LastName,
            CourseId = e.CourseId,
            CourseTitle = e.Course.Title,
            Status = e.Status.ToString(),
            VideoWatched = e.VideoWatched,
            VideoProgressSeconds = e.VideoProgressSeconds,
            InvitedAt = e.InvitedAt,
            CompletedAt = e.CompletedAt,
            BestScore = e.QuizAttempts.Any() ? e.QuizAttempts.Max(a => a.Score) : 0,
            AttemptCount = e.QuizAttempts.Count
        }).ToList();

        return Ok(result);
    }
}
