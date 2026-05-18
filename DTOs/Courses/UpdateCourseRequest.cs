using System.ComponentModel.DataAnnotations;

namespace lms_api.DTOs.Courses;

public class UpdateCourseRequest
{
    [MaxLength(200)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    public bool? RequireFullVideoWatch { get; set; }

    public bool? IsActive { get; set; }
}
