namespace TawtheefTest.ViewModels
{
  public class AddToExamDTO
  {
    public long QuestionSetId { get; set; }
    public long ExamId { get; set; }
    public long DisplayOrder { get; set; } = 1;
  }
}
