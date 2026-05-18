namespace lms_api.Models;

public class QuizAttempt
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public int Score { get; set; }
    public bool Passed { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    public string AnswersJson { get; set; } = "{}";
    public Enrollment Enrollment { get; set; } = null!;
}
