namespace lms_api.DTOs.Courses;

public class VideoProgressRequest
{
    public int ProgressSeconds { get; set; }
    public bool MarkWatched { get; set; } = false;
}
