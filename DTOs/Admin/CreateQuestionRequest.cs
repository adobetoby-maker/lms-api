using System.ComponentModel.DataAnnotations;

namespace lms_api.DTOs.Admin;

public class CreateQuestionRequest
{
    [Required]
    public string Text { get; set; } = string.Empty;

    [Required]
    public string OptionA { get; set; } = string.Empty;

    [Required]
    public string OptionB { get; set; } = string.Empty;

    [Required]
    public string OptionC { get; set; } = string.Empty;

    [Required]
    public string OptionD { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^[ABCD]$", ErrorMessage = "CorrectAnswer must be A, B, C, or D")]
    public string CorrectAnswer { get; set; } = string.Empty;

    public int SortOrder { get; set; } = 0;
}
