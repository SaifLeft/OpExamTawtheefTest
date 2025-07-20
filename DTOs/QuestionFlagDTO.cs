using System;

namespace TawtheefTest.DTOs
{
  public class QuestionFlagDTO
  {
    public long CandidateExamId { get; set; }
    public long QuestionId { get; set; }
    public long IsFlagged { get; set; }
  }
}
