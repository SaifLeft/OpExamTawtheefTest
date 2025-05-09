using System;
using System.Collections.Generic;

namespace TawtheefTest.ViewModels
{
  public class CandidateAnswerViewModel
  {
    public int Id { get; set; }
    public int CandidateExamId { get; set; }
    public int QuestionId { get; set; }
    public string QuestionText { get; set; }
    public string QuestionType { get; set; }
    public string AnswerText { get; set; }
    public bool? IsCorrect { get; set; }
    public string CorrectAnswerText { get; set; }
    public string CorrectAnswer { get; set; }
    public DateTime? SubmittedAt { get; set; }

    // الخصائص المضافة لإصلاح الأخطاء
    public string Answer { get; set; }
    public List<CandidateQuestionOptionViewModel> Options { get; set; } = new List<CandidateQuestionOptionViewModel>();
  }
}
