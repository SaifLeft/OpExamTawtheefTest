using TawtheefTest.Enums;

public class SaveAnswerDTO
{
  public long CandidateExamId { get; set; }
  public long QuestionId { get; set; }
  public string? AnswerText { get; set; }
  public QuestionTypeEnum QuestionType { get; set; }
  public List<int>? SelectedOptionsIds { get; set; }
  public List<int>? OrderingItemsIds { get; set; }
  public List<int>? MatchingPairsIds { get; set; }
  public long IsCompleted { get; set; }
  public long IsFlagged { get; set; }
}
