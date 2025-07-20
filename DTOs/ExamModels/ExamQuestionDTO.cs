using System.Collections.Generic;

namespace TawtheefTest.DTOs.ExamModels
{
  public class ExamQuestionDTO
  {
    public long Id { get; set; }
    public long SequenceNumber { get; set; }
    public string QuestionText { get; set; }
    public string QuestionType { get; set; }
    public List<QuestionOptionDTO> Options { get; set; }
    public string Answer { get; set; }
    public bool? TrueFalseAnswer { get; set; }

    // للأسئلة من نوع الترتيب
    public List<string> CorrectlyOrdered { get; set; }
    public List<string> ShuffledOrder { get; set; }
    public string InstructionText { get; set; }

    // للأسئلة من نوع المطابقة
    public List<MatchingPairDTO> MatchingPairs { get; set; }

  }

  public class QuestionOptionDTO
  {
    public long Id { get; set; }
    public string Text { get; set; }
    public long Index { get; set; }
    public long IsCorrect { get; set; }
  }

  public class MatchingPairDTO
  {
    public string Left { get; set; }
    public string Right { get; set; }
    public long Index { get; set; }
  }
}
