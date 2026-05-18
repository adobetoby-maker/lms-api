namespace lms_api.Models;

public enum EnrollmentStatus { Invited, InProgress, Passed, Failed }

public class Enrollment
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Invited;
    public bool VideoWatched { get; set; } = false;
    public int VideoProgressSeconds { get; set; } = 0;
    public DateTime InvitedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Course Course { get; set; } = null!;
    public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
}
