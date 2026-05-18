using System.Security.Claims;
using System.Text.Json;
using lms_api.Data;
using lms_api.DTOs.Quiz;
using lms_api.Models;
using lms_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lms_api.Controllers;

[ApiController]
[Route("api/courses/{courseId:int}/quiz")]
[Authorize]
public class QuizController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<QuizController> _logger;

    public QuizController(
        AppDbContext db,
        IEmailService emailService,
        UserManager<ApplicationUser> userManager,
        ILogger<QuizController> logger)
    {
        _db = db;
        _emailService = emailService;
        _userManager = userManager;
        _logger = logger;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();

    /// <summary>Get quiz questions WITHOUT correct answers.</summary>
    [HttpGet]
    public async Task<ActionResult<List<QuizQuestionDto>>> GetQuiz(int courseId)
    {
        string userId = GetUserId();

        Enrollment? enrollment = await _db.Enrollments
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

        if (enrollment == null)
            return Forbid();

        // Enforce video watch requirement
        if (enrollment.Course.RequireFullVideoWatch && !enrollment.VideoWatched)
            return BadRequest(new { message = "You must watch the full video before taking the quiz." });

        List<Question> questions = await _db.Questions
            .Where(q => q.CourseId == courseId)
            .OrderBy(q => q.SortOrder)
            .ToListAsync();

        List<QuizQuestionDto> dto = questions.Select(q => new QuizQuestionDto
        {
            Id = q.Id,
            Text = q.Text,
            OptionA = q.OptionA,
            OptionB = q.OptionB,
            OptionC = q.OptionC,
            OptionD = q.OptionD,
            SortOrder = q.SortOrder
        }).ToList();

        return Ok(dto);
    }

    /// <summary>Submit quiz answers, grade them, and update enrollment status.</summary>
    [HttpPost("submit")]
    public async Task<ActionResult<QuizResultDto>> SubmitQuiz(int courseId, [FromBody] SubmitQuizRequest request)
    {
        string userId = GetUserId();

        Enrollment? enrollment = await _db.Enrollments
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

        if (enrollment == null)
            return Forbid();

        if (enrollment.Course.RequireFullVideoWatch && !enrollment.VideoWatched)
            return BadRequest(new { message = "You must watch the full video before submitting the quiz." });

        List<Question> questions = await _db.Questions
            .Where(q => q.CourseId == courseId)
            .ToListAsync();

        if (questions.Count == 0)
            return BadRequest(new { message = "This course has no quiz questions." });

        // Grade the quiz
        int correctCount = 0;
        Dictionary<int, string> correctAnswers = new Dictionary<int, string>();

        foreach (Question question in questions)
        {
            correctAnswers[question.Id] = question.CorrectAnswer;
            if (request.Answers.TryGetValue(question.Id, out string? givenAnswer))
            {
                if (string.Equals(givenAnswer, question.CorrectAnswer, StringComparison.OrdinalIgnoreCase))
                    correctCount++;
            }
        }

        int score = (int)Math.Round((double)correctCount / questions.Count * 100);
        bool passed = score >= 80; // 80% passing threshold

        // Save quiz attempt
        string answersJson = JsonSerializer.Serialize(request.Answers);
        QuizAttempt attempt = new QuizAttempt
        {
            EnrollmentId = enrollment.Id,
            Score = score,
            Passed = passed,
            AttemptedAt = DateTime.UtcNow,
            AnswersJson = answersJson
        };
        _db.QuizAttempts.Add(attempt);

        // Update enrollment status
        if (passed)
        {
            enrollment.Status = EnrollmentStatus.Passed;
            enrollment.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            enrollment.Status = EnrollmentStatus.Failed;
        }

        await _db.SaveChangesAsync();

        // Send completion email if passed
        if (passed)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                string userName = $"{user.FirstName} {user.LastName}".Trim();
                await _emailService.SendCompletionEmailAsync(
                    user.Email ?? string.Empty,
                    userName,
                    enrollment.Course.Title);
            }
        }

        string message = passed
            ? $"Congratulations! You passed with {score}%."
            : $"You scored {score}%. A passing score of 80% is required. You may try again.";

        return Ok(new QuizResultDto
        {
            Score = score,
            Passed = passed,
            TotalQuestions = questions.Count,
            CorrectAnswers = correctCount,
            Message = message,
            CorrectAnswers_ = correctAnswers
        });
    }
}
