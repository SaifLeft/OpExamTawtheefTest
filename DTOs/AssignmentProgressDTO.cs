namespace TawtheefTest.DTOs
{
  public class QuestionMetadataDTO
  {
    public int QuestionId { get; set; }
    public int QuestionIndex { get; set; }
    public string QuestionType { get; set; }
    public bool IsAnswered { get; set; }
    public bool IsFlagged { get; set; }
    public DateTime? LastAnsweredAt { get; set; }
    public int TimeSpentSeconds { get; set; }
  }

  public class AssignmentProgressDTO
  {
    public int TotalQuestions { get; set; }
    public int CompletedQuestions { get; set; }
    public int FlaggedQuestions { get; set; }
    public int RemainingQuestions { get; set; }
    public double ProgressPercentage { get; set; }
    public TimeSpan? RemainingTime { get; set; }
    public bool IsNearCompletion { get; set; }
    public bool HasCriticalTimeRemaining { get; set; }
  }
}
