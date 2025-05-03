using System;
using System.Collections.Generic;
using TawtheefTest.Enum;
using TawtheefTest.ViewModels;

namespace TawtheefTest.DTOs
{
  public class QuestionSetDto
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string QuestionType { get; set; }
    public string Language { get; set; }
    public string Difficulty { get; set; }
    public int QuestionCount { get; set; }
    public int? OptionsCount { get; set; }
    public QuestionSetStatus Status { get; set; }
    public string StatusDescription { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int QuestionsGenerated { get; set; }
    public int ExamId { get; set; }
    public IEnumerable<ContentSourceViewModel> ContentSources { get; set; }
    public IEnumerable<QuestionViewModel> Questions { get; set; }
  }
}
