namespace lms_api.Models;

public class Question
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public int SortOrder { get; set; } = 0;
    public Course Course { get; set; } = null!;
}
