using System;

namespace TawtheefTest.ViewModels
{
  public class CandidateQuestionOptionViewModel
  {
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string Text { get; set; }
    public bool IsCorrect { get; set; }
    public bool IsSelected { get; set; }
  }
}
