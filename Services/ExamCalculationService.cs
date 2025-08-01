using TawtheefTest.Data.Structure;
using TawtheefTest.DTOs;
using TawtheefTest.Enums;

namespace TawtheefTest.Services
{
  // IExamCalculationService.cs
  public interface IExamCalculationService
  {
    TimeSpan? CalculateRemainingTime(Assignment assignment);
    int CalculateProgressPercentage(Assignment assignment, List<CandidateAnswer> candidateAnswers);
    decimal CalculateScore(Assignment assignment);

    // الطريقة الجديدة
    AssignmentProgressStatistics GetProgressStatistics(Assignment assignment);

    // طرق مساعدة إضافية
    bool IsAssignmentExpired(Assignment assignment);
    TimeSpan GetTimeSpent(Assignment assignment);
    double GetCompletionRate(Assignment assignment);
  }

  // ExamCalculationService.cs
  public class ExamCalculationService : IExamCalculationService
  {
    public AssignmentProgressStatistics GetProgressStatistics(Assignment assignment)
    {
      if (assignment == null)
        throw new ArgumentNullException(nameof(assignment));

      var statistics = new AssignmentProgressStatistics();

      // معلومات أساسية
      statistics.TotalQuestions = assignment.TotalQuestions;
      statistics.CompletedQuestions = assignment.CompletedQuestions;
      statistics.RemainingQuestions = statistics.TotalQuestions - statistics.CompletedQuestions;

      // حساب النسبة المئوية للتقدم
      statistics.ProgressPercentage = statistics.TotalQuestions > 0
          ? Math.Round((double)statistics.CompletedQuestions / statistics.TotalQuestions * 100, 2)
          : 0;

      // الأسئلة المعلمة للمراجعة
      statistics.FlaggedQuestions = assignment.CandidateAnswers?.Count(ca => ca.IsFlagged) ?? 0;

      // معلومات الوقت
      CalculateTimeStatistics(assignment, statistics);

      // إحصائيات حسب مستوى الصعوبة
      CalculateDifficultyStatistics(assignment, statistics);

      // حساب متوسط الوقت لكل سؤال
      CalculatePerformanceMetrics(assignment, statistics);

      // تحديد التحذيرات والحالات الخاصة
      DetermineWarningsAndStatus(assignment, statistics);

      // الحالة الحالية
      Enum.TryParse<AssignmentStatus>(assignment.Status, out var status);
      statistics.CurrentStatus = status;

      return statistics;
    }

    private void CalculateTimeStatistics(Assignment assignment, AssignmentProgressStatistics statistics)
    {
      // الوقت المتبقي
      statistics.RemainingTime = CalculateRemainingTime(assignment);
      statistics.IsOverTime = statistics.RemainingTime?.TotalSeconds <= 0;

      // إجمالي الوقت والوقت المستغرق
      statistics.TotalTimeMinutes = assignment.Exam?.Duration ?? 0;

      if (assignment.StartTime.HasValue)
      {
        var timeSpent = GetTimeSpent(assignment);
        statistics.TimeSpentMinutes = (int)timeSpent.TotalMinutes;

        // نسبة تقدم الوقت
        if (statistics.TotalTimeMinutes > 0)
        {
          statistics.TimeProgressPercentage = Math.Round(
              (double)statistics.TimeSpentMinutes / statistics.TotalTimeMinutes * 100, 2);
        }
      }

      // تحديد الوقت الحرج (أقل من 10 دقائق)
      statistics.HasCriticalTimeRemaining = statistics.RemainingTime?.TotalMinutes <= 10
                                          && statistics.RemainingTime?.TotalMinutes > 0;
    }

    private void CalculateDifficultyStatistics(Assignment assignment, AssignmentProgressStatistics statistics)
    {
      if (assignment.CandidateAnswers?.Any() != true) return;

      var answeredQuestions = assignment.CandidateAnswers
          .Where(ca => !string.IsNullOrEmpty(ca.AnswerText))
          .Select(ca => ca.Question)
          .Where(q => q != null);

      statistics.EasyQuestionsCompleted = answeredQuestions
          .Count(q => q.DifficultyLevel?.ToLower() == "easy");

      statistics.MediumQuestionsCompleted = answeredQuestions
          .Count(q => q.DifficultyLevel?.ToLower() == "medium");

      statistics.HardQuestionsCompleted = answeredQuestions
          .Count(q => q.DifficultyLevel?.ToLower() == "hard");
    }

    private void CalculatePerformanceMetrics(Assignment assignment, AssignmentProgressStatistics statistics)
    {
      if (assignment.StartTime.HasValue && statistics.CompletedQuestions > 0)
      {
        var timeSpent = GetTimeSpent(assignment);
        statistics.AverageTimePerQuestion = Math.Round(
            timeSpent.TotalMinutes / statistics.CompletedQuestions, 2);

        // تقدير وقت الانتهاء
        if (statistics.RemainingQuestions > 0 && statistics.AverageTimePerQuestion > 0)
        {
          var estimatedRemainingMinutes = statistics.RemainingQuestions * statistics.AverageTimePerQuestion;
          var estimatedCompletion = DateTime.Now.AddMinutes(estimatedRemainingMinutes);
          statistics.EstimatedCompletionTime = estimatedCompletion.ToString("HH:mm");
        }
      }

      // تحديد ما إذا كان قريب من الانتهاء
      statistics.IsNearCompletion = statistics.ProgressPercentage >= 80;
    }

    private void DetermineWarningsAndStatus(Assignment assignment, AssignmentProgressStatistics statistics)
    {
      var warnings = new List<string>();

      // تحذيرات الوقت
      if (statistics.HasCriticalTimeRemaining)
      {
        warnings.Add("تبقى أقل من 10 دقائق على انتهاء الاختبار");
      }
      else if (statistics.RemainingTime?.TotalMinutes <= 30 && statistics.RemainingTime?.TotalMinutes > 10)
      {
        warnings.Add("تبقى أقل من 30 دقيقة على انتهاء الاختبار");
      }

      if (statistics.IsOverTime)
      {
        warnings.Add("انتهى الوقت المخصص للاختبار");
      }

      // تحذيرات التقدم
      if (statistics.TimeProgressPercentage > 50 && statistics.ProgressPercentage < 25)
      {
        warnings.Add("تقدمك في الإجابة أبطأ من المتوقع");
      }

      if (statistics.FlaggedQuestions > statistics.TotalQuestions * 0.3)
      {
        warnings.Add($"لديك {statistics.FlaggedQuestions} أسئلة معلمة للمراجعة");
      }

      // تحذيرات الأداء
      if (statistics.AverageTimePerQuestion > 0)
      {
        var expectedAverageTime = (double)statistics.TotalTimeMinutes / statistics.TotalQuestions;
        if (statistics.AverageTimePerQuestion > expectedAverageTime * 1.5)
        {
          warnings.Add("تستغرق وقتاً أطول من المتوقع في كل سؤال");
        }
      }

      // تحذير عدم الإجابة
      if (statistics.CompletedQuestions == 0 && statistics.TimeSpentMinutes > 10)
      {
        warnings.Add("لم تجب على أي سؤال بعد");
      }

      statistics.Warnings = warnings;
      statistics.NeedsAttention = warnings.Any() || statistics.HasCriticalTimeRemaining || statistics.IsOverTime;
    }

    public TimeSpan? CalculateRemainingTime(Assignment assignment)
    {
      if (!assignment.StartTime.HasValue || assignment.Exam?.Duration == null)
        return null;

      var endTime = assignment.StartTime.Value.AddMinutes(assignment.Exam.Duration);
      var remaining = endTime - DateTime.Now;

      return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
    }

    public TimeSpan GetTimeSpent(Assignment assignment)
    {
      if (!assignment.StartTime.HasValue)
        return TimeSpan.Zero;

      var endTime = assignment.EndTime ?? DateTime.Now;
      return endTime - assignment.StartTime.Value;
    }

    public int CalculateProgressPercentage(Assignment assignment, List<CandidateAnswer> candidateAnswers)
    {
      if (assignment.TotalQuestions <= 0)
        return 0;

      var completedCount = candidateAnswers?.Count(ca => !string.IsNullOrEmpty(ca.AnswerText)) ?? 0;
      return (int)Math.Round((double)completedCount / assignment.TotalQuestions * 100);
    }

    public double GetCompletionRate(Assignment assignment)
    {
      if (assignment.TotalQuestions <= 0)
        return 0;

      return (double)assignment.CompletedQuestions / assignment.TotalQuestions;
    }

    public bool IsAssignmentExpired(Assignment assignment)
    {
      var remainingTime = CalculateRemainingTime(assignment);
      return remainingTime?.TotalSeconds <= 0;
    }

    public decimal CalculateScore(Assignment assignment)
    {
      // منطق حساب النتيجة - يمكن تخصيصه حسب المتطلبات
      if (assignment.CandidateAnswers?.Any() != true)
        return 0;

      var totalQuestions = assignment.TotalQuestions;
      var correctAnswers = assignment.CandidateAnswers.Count(ca => ca.IsCorrect == true);

      if (totalQuestions <= 0)
        return 0;

      return Math.Round((decimal)correctAnswers / totalQuestions * 100, 2);
    }



  }
}
