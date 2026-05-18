using System.ComponentModel.DataAnnotations;

namespace lms_api.DTOs.Courses;

public class CreateCourseRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public bool RequireFullVideoWatch { get; set; } = false;
    public bool IsActive { get; set; } = true;
}
