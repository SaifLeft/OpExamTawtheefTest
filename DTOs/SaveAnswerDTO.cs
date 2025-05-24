using TawtheefTest.Enums;

public class SaveAnswerDTO
{
  public int CandidateExamId { get; set; }
  public int QuestionId { get; set; }
  public string? AnswerText { get; set; }
  public QuestionTypeEnum QuestionType { get; set; }
  public List<int>? SelectedOptionsIds { get; set; }
  public List<int>? OrderingItemsIds { get; set; }
  public List<int>? MatchingPairsIds { get; set; }
  public bool IsCompleted { get; set; }
  public bool IsFlagged { get; set; }
}
