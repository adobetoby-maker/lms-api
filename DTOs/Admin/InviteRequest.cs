using System.ComponentModel.DataAnnotations;

namespace lms_api.DTOs.Admin;

public class InviteRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public int CourseId { get; set; }
}
