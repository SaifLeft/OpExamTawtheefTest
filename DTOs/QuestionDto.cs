using TawtheefTest.ViewModels;

namespace TawtheefTest.DTOs
{
  public class QuestionDto
  {
    public int Id { get; set; }
    public int Index { get; set; }
    public string QuestionText { get; set; }
    public string QuestionType { get; set; }
    public IEnumerable<QuestionOptionViewModel> Options { get; set; }
    public IEnumerable<MatchingPairViewModel> MatchingPairs { get; set; }
    public IEnumerable<OrderingItemViewModel> OrderingItems { get; set; }
    public IEnumerable<string> CorrectlyOrdered { get; set; }
    public bool? TrueFalseAnswer { get; set; }
    public string Answer { get; set; }
    public string InstructionText { get; set; }
    public string SampleAnswer { get; set; }
  }
}
