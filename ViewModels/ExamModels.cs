using System;
using System.Collections.Generic;

namespace TawtheefTest.ViewModels
{
  public class ExamForCandidateViewModel
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int JobId { get; set; }
    public string JobName { get; set; }
    public int? Duration { get; set; }
    public string Status { get; set; }
    public decimal? PassPercentage { get; set; }
    public int TotalQuestions { get; set; }
    public DateTime CreatedAt { get; set; }

    // الخصائص المضافة لإصلاح الأخطاء
    public string QuestionType { get; set; }
    public string Difficulty { get; set; }
    public int QuestionCountForEachCandidate { get; set; }
  }

  public class CandidateExamViewModel
  {
    public int Id { get; set; }
    public int CandidateId { get; set; }
    public int ExamId { get; set; }
    public string ExamTitle { get; set; }
    public string Status { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal? Score { get; set; }
    public string CandidateName { get; set; }
    public string JobTitle { get; set; }
    public int Duration { get; set; }
    public int TotalQuestions { get; set; }
    public int CompletedQuestions { get; set; }
    public TimeSpan? RemainingTime { get; set; }
    public List<CandidateAnswerViewModel> Answers { get; set; }
    public List<CandidateQuestionViewModel> Questions { get; set; } = new List<CandidateQuestionViewModel>();
    public List<int> FlaggedQuestions { get; set; } = new List<int>();
    public int CurrentQuestionIndex { get; set; } = 0;

    // الخاصية المضافة لإصلاح الأخطاء
    public bool IsCompleted => Status == "Completed";

    // خصائص إضافية لإصلاح أخطاء Index.cshtml
    public string ExamName => ExamTitle;
    public string JobName => JobTitle;
    public string QuestionType { get; set; }
  }

  public class CandidateExamResultViewModel
  {
    public int Id { get; set; }
    public int CandidateId { get; set; }
    public int ExamId { get; set; }
    public string ExamTitle { get; set; }
    public string Status { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal? Score { get; set; }
    public string CandidateName { get; set; }
    public string JobTitle { get; set; }
    public int CompletedQuestions { get; set; }
    public int TotalQuestions { get; set; }
    public TimeSpan? Duration { get; set; }
    public List<CandidateAnswerViewModel> Answers { get; set; } = new List<CandidateAnswerViewModel>();
    public bool HasPassed => Score.HasValue && Score.Value >= 60;

    // الخاصية المضافة لإصلاح الأخطاء
    public bool ShowResultsImmediately { get; set; } = false;
  }
}
