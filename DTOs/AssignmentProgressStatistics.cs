using TawtheefTest.Enums;

namespace TawtheefTest.DTOs
{
  public class AssignmentProgressStatistics
  {
    public int TotalQuestions { get; set; }
    public int CompletedQuestions { get; set; }
    public int FlaggedQuestions { get; set; }
    public int RemainingQuestions { get; set; }
    public double ProgressPercentage { get; set; }
    public TimeSpan? RemainingTime { get; set; }
    public bool IsNearCompletion { get; set; }
    public bool HasCriticalTimeRemaining { get; set; }
    public bool IsOverTime { get; set; }
    public int TimeSpentMinutes { get; set; }
    public int TotalTimeMinutes { get; set; }
    public double TimeProgressPercentage { get; set; }

    // إحصائيات حسب نوع السؤال
    public int EasyQuestionsCompleted { get; set; }
    public int MediumQuestionsCompleted { get; set; }
    public int HardQuestionsCompleted { get; set; }

    // معلومات الأداء
    public double AverageTimePerQuestion { get; set; }
    public string EstimatedCompletionTime { get; set; }
    public AssignmentStatus CurrentStatus { get; set; }

    // تحذيرات
    public List<string> Warnings { get; set; } = new List<string>();
    public bool NeedsAttention { get; set; }
  }
}
