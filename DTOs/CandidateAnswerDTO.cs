using System;
using System.Collections.Generic;

namespace TawtheefTest.DTOs
{
  public class CandidateAnswerDTO
  {
    public long Id { get; set; }
    public long CandidateExamId { get; set; }
    public long QuestionId { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    public long? SelectedOptionId { get; set; }
    public bool? IsCorrect { get; set; }
    public long IsFlagged { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
  }
}
