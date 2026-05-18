namespace lms_api.DTOs.Admin;

public class EnrollmentSummaryDto
{
    public int EnrollmentId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string UserFirstName { get; set; } = string.Empty;
    public string UserLastName { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool VideoWatched { get; set; }
    public int VideoProgressSeconds { get; set; }
    public DateTime InvitedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int BestScore { get; set; }
    public int AttemptCount { get; set; }
}
