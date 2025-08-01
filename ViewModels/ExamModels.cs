using System;
using System.Collections.Generic;
using TawtheefTest.Data.Structure;
using TawtheefTest.Enums;

namespace TawtheefTest.ViewModels
{
  public class ExamForCandidateViewModel
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int JobId { get; set; }
    public string JobName { get; set; }
    public long? Duration { get; set; }
    public string Status { get; set; }
    public decimal? PassPercentage { get; set; }
    public int TotalQuestions { get; set; }
    public DateTime StartTime { get; set; }

    // الخصائص المضافة لإصلاح الأخطاء
    public string QuestionType { get; set; }
    public string Difficulty { get; set; }
    public int QuestionCountForEachCandidate { get; set; }
  }

  public class AssignmentViewModel
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
    public List<CandidateAnswerViewModel> Answers { get; set; } = new List<CandidateAnswerViewModel>();
    public List<CandidateQuestionViewModel> Questions { get; set; } = new List<CandidateQuestionViewModel>();
    public List<int> FlaggedQuestions { get; set; } = new List<int>();
    public int CurrentQuestionIndex { get; set; } = 0;
    public int ProgressPercentage { get; set; }
    public List<CandidateAnswer> CandidateAnswers { get; set; } = new List<CandidateAnswer>();

    // خصائص محسّنة
    public bool IsCompleted => Status == AssignmentStatus.Completed.ToString();
    public string ExamName => ExamTitle;
    public string JobName => JobTitle;
    public int RemainingQuestions => TotalQuestions - CompletedQuestions;
    public bool HasTimeRemaining => RemainingTime.HasValue && RemainingTime.Value.TotalSeconds > 0;
    public string FormattedRemainingTime => RemainingTime?.ToString(@"hh\:mm\:ss") ?? "00:00:00";

    // خصائص للإحصائيات
    public double CompletionRate => TotalQuestions > 0 ? (double)CompletedQuestions / TotalQuestions * 100 : 0;
    public bool IsNearlyComplete => CompletionRate >= 80;
    public bool HasFlaggedQuestions => FlaggedQuestions?.Any() == true;
  }

  public class AssignmentResultViewModel
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

  /// <summary>
  /// نموذج عرض لصفحة تعليمات الامتحان قبل البدء
  /// </summary>
  public class ExamInstructionsViewModel
  {
    public int ExamId { get; set; }
    public string ExamName { get; set; }
    public string JobName { get; set; }
    public int Duration { get; set; } // مدة الامتحان بالدقائق
    public int TotalQuestions { get; set; } // إجمالي عدد الأسئلة
    public string CandidateName { get; set; }
    public List<string> ExamRules { get; set; } = new List<string>(); // قواعد الامتحان
    public List<string> TechnicalInstructions { get; set; } = new List<string>(); // التعليمات التقنية
    public bool HasExistingAttempt { get; set; } // هل يوجد محاولة سابقة
    public long? ExistingAttemptId { get; set; } // معرف المحاولة السابقة إن وجدت
  }
}
