using System;
using System.Collections.Generic;
using TawtheefTest.Enums;

namespace TawtheefTest.Models
{
  public class QuestionSet
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
    public DateTime CreatedDate { get; set; }

    // Navigation properties
    public ICollection<QuestionDTO> Questions { get; set; }
    public ICollection<ExamQuestionSet> ExamQuestionSets { get; set; }
  }
}
