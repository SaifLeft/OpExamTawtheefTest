using System;

namespace TawtheefTest.Models
{
  public class Option
  {
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string OptionText { get; set; }
    public bool IsCorrect { get; set; }
    public int OrderPosition { get; set; }

    // Navigation property
    public QuestionDTO Question { get; set; }
  }
}
