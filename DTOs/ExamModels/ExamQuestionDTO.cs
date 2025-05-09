using System.Collections.Generic;

namespace TawtheefTest.DTOs.ExamModels
{
  public class ExamQuestionDTO
  {
    public int Id { get; set; }
    public int SequenceNumber { get; set; }
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
    public int Id { get; set; }
    public string Text { get; set; }
    public int Index { get; set; }
    public bool IsCorrect { get; set; }
  }

  public class MatchingPairDTO
  {
    public string Left { get; set; }
    public string Right { get; set; }
    public int Index { get; set; }
  }
}
