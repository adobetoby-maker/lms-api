namespace lms_api.Models;

public class Course
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RequireFullVideoWatch { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Video> Videos { get; set; } = new List<Video>();
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
