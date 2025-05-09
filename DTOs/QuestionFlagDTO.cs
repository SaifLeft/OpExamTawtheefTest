using System;

namespace TawtheefTest.DTOs
{
  public class QuestionFlagDTO
  {
    public int CandidateExamId { get; set; }
    public int QuestionId { get; set; }
    public bool IsFlagged { get; set; }
  }
}
