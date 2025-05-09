using System;
using System.Collections.Generic;

namespace TawtheefTest.ViewModels
{
  public class CandidateQuestionViewModel
  {
    public int Id { get; set; }
    public string QuestionType { get; set; }
    public string QuestionText { get; set; }
    public int DisplayOrder { get; set; }
    public List<CandidateQuestionOptionViewModel> Options { get; set; }
    public bool IsAnswered { get; set; }
    public bool IsFlagged { get; set; } = false;
  }
}
