namespace lms_api.Models;

public enum VideoType { YouTube, Vimeo, Upload }

public class Video
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public VideoType Type { get; set; }
    public string Url { get; set; } = string.Empty;
    public int DurationSeconds { get; set; } = 0;
    public int SortOrder { get; set; } = 0;
    public Course Course { get; set; } = null!;
}
