using System.ComponentModel.DataAnnotations;
using lms_api.Models;

namespace lms_api.DTOs.Admin;

public class CreateVideoRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public VideoType Type { get; set; }

    [Required]
    public string Url { get; set; } = string.Empty;

    public int DurationSeconds { get; set; } = 0;
    public int SortOrder { get; set; } = 0;
}
