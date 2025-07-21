using System;
using System.Collections.Generic;
using TawtheefTest.Enums;

namespace TawtheefTest.ViewModels
{
  public class QuestionSetDetailsViewModel
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string QuestionType { get; set; }
    public string Language { get; set; }
    public string Difficulty { get; set; }
    public int QuestionCount { get; set; }
    public long? OptionsCount { get; set; }
    public QuestionSetStatus Status { get; set; }
    public string StatusDescription { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    // خصائص مصدر المحتوى مباشرة بدلاً من ContentSources
    public string ContentSourceType { get; set; }
    public string Content { get; set; }
    public string Url { get; set; }
    public string FileName { get; set; }
    public string FileUploadedCode { get; set; }

    public IEnumerable<QuestionViewModel> Questions { get; set; }
    public int ExamId { get; set; }
  }

  public class QuestionSetStatusViewModel
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public QuestionSetStatus Status { get; set; }
    public string StatusDescription { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int QuestionsGenerated { get; set; }
  }

  public class QuestionViewModel
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

  public class QuestionOptionViewModel
  {
    public int Id { get; set; }
    public int Index { get; set; }
    public string Text { get; set; }
    public bool IsCorrect { get; set; }
  }

  public class MatchingPairViewModel
  {
    public int Id { get; set; }
    public string LeftSide { get; set; }
    public string RightSide { get; set; }
    public int DisplayOrder { get; set; }
  }

  public class OrderingItemViewModel
  {
    public int Id { get; set; }
    public string Text { get; set; }
    public int CorrectOrder { get; set; }
    public int DisplayOrder { get; set; }
  }
}
