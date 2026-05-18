namespace lms_api.DTOs.Courses;

public class VideoDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public int SortOrder { get; set; }
}

public class CourseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RequireFullVideoWatch { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<VideoDto> Videos { get; set; } = new List<VideoDto>();
    public int QuestionCount { get; set; }
}

public class CourseListDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RequireFullVideoWatch { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int VideoCount { get; set; }
    public int QuestionCount { get; set; }
    public string EnrollmentStatus { get; set; } = string.Empty;
    public bool VideoWatched { get; set; }
    public int VideoProgressSeconds { get; set; }
}
