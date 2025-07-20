using System;
using System.Collections.Generic;

namespace TawtheefTest.ViewModels
{
  public class ExamForCandidateViewModel
  {
    public long Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public long JobId { get; set; }
    public string JobName { get; set; }
    public long? Duration { get; set; }
    public string Status { get; set; }
    public decimal? PassPercentage { get; set; }
    public long TotalQuestions { get; set; }
    public DateTime StartTime { get; set; }

    // الخصائص المضافة لإصلاح الأخطاء
    public string QuestionType { get; set; }
    public string Difficulty { get; set; }
    public long QuestionCountForEachCandidate { get; set; }
  }

  public class CandidateExamViewModel
  {
    public long Id { get; set; }
    public long CandidateId { get; set; }
    public long ExamId { get; set; }
    public string ExamTitle { get; set; }
    public string Status { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Score { get; set; }
    public string CandidateName { get; set; }
    public string JobTitle { get; set; }
    public long Duration { get; set; }
    public long TotalQuestions { get; set; }
    public long CompletedQuestions { get; set; }
    public TimeSpan? RemainingTime { get; set; }
    public List<CandidateAnswerViewModel> Answers { get; set; }
    public List<CandidateQuestionViewModel> Questions { get; set; } = new List<CandidateQuestionViewModel>();
    public List<int> FlaggedQuestions { get; set; } = new List<int>();
    public long CurrentQuestionIndex { get; set; } = 0;

    // الخاصية المضافة لإصلاح الأخطاء
    public bool IsCompleted => Status == "Completed";

    // خصائص إضافية لإصلاح أخطاء Index.cshtml
    public string ExamName => ExamTitle;
    public string JobName => JobTitle;
  }

  public class CandidateExamResultViewModel
  {
    public long Id { get; set; }
    public long CandidateId { get; set; }
    public long ExamId { get; set; }
    public string ExamTitle { get; set; }
    public string Status { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal? Score { get; set; }
    public string CandidateName { get; set; }
    public string JobTitle { get; set; }
    public long CompletedQuestions { get; set; }
    public long TotalQuestions { get; set; }
    public TimeSpan? Duration { get; set; }
    public List<CandidateAnswerViewModel> Answers { get; set; } = new List<CandidateAnswerViewModel>();
    public bool HasPassed => Score.HasValue && Score.Value >= 60;

    // الخاصية المضافة لإصلاح الأخطاء
    public bool ShowResultsImmediately { get; set; } = false;
  }

  /// <summary>
  /// نموذج عرض لصفحة تعليمات الامتحان قبل البدء
  /// </summary>
  public class ExamInstructionsViewModel
  {
    public long ExamId { get; set; }
    public string ExamName { get; set; }
    public string ExamDescription { get; set; }
    public string JobName { get; set; }
    public long Duration { get; set; } // مدة الامتحان بالدقائق
    public long TotalQuestions { get; set; } // إجمالي عدد الأسئلة
    public decimal? PassPercentage { get; set; } // نسبة النجاح المطلوبة
    public string CandidateName { get; set; }
    public List<string> ExamRules { get; set; } = new List<string>(); // قواعد الامتحان
    public List<string> TechnicalInstructions { get; set; } = new List<string>(); // التعليمات التقنية
    public long HasExistingAttempt { get; set; } // هل يوجد محاولة سابقة
    public long? ExistingAttemptId { get; set; } // معرف المحاولة السابقة إن وجدت
  }
}
