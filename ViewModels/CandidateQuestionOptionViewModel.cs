using System;

namespace TawtheefTest.ViewModels
{
  public class CandidateQuestionOptionViewModel
  {
    public long Id { get; set; }
    public long QuestionId { get; set; }
    public string Text { get; set; }
    public long IsCorrect { get; set; }
    public long IsSelected { get; set; }
  }
}
