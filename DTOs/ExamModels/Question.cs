namespace TawtheefTest.DTOs.ExamModels
{
  public class QuestionDTO
  {
    public string QuestionText { get; set; }
    public string QuestionType { get; set; }
    public List<OptionDTO> Options { get; set; }
    public string CorrectAnswer { get; set; }

    // For matching questions
    public List<string> LeftColumn { get; set; }
    public List<string> RightColumn { get; set; }
  }

  public class OptionDTO
  {
    public string OptionText { get; set; }
    public bool IsCorrect { get; set; }
    public int OrderPosition { get; set; }
  }
}
