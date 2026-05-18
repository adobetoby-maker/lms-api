namespace lms_api.Models;

public class Invite
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string Token { get; set; } = Guid.NewGuid().ToString("N");
    public bool IsUsed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);
    public Course Course { get; set; } = null!;
}
