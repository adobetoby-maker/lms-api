using System.ComponentModel.DataAnnotations;

namespace lms_api.DTOs.Quiz;

public class SubmitQuizRequest
{
    /// <summary>Maps questionId → answer letter ("A", "B", "C", or "D").</summary>
    [Required]
    public Dictionary<int, string> Answers { get; set; } = new Dictionary<int, string>();
}
