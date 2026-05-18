namespace lms_api.DTOs.Quiz;

public class QuizQuestionDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class QuizResultDto
{
    public int Score { get; set; }
    public bool Passed { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<int, string> CorrectAnswers_ { get; set; } = new Dictionary<int, string>();
}
