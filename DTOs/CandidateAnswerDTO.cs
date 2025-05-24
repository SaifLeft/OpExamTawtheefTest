using System;
using System.Collections.Generic;

namespace TawtheefTest.DTOs
{
  public class CandidateAnswerDTO
  {
    public int Id { get; set; }
    public int CandidateExamId { get; set; }
    public int QuestionId { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    public int? SelectedOptionId { get; set; }
    public bool? IsCorrect { get; set; }
    public bool IsFlagged { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
  }
}
