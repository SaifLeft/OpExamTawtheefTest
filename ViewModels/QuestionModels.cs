using System;
using System.Collections.Generic;
using TawtheefTest.Enums;

namespace TawtheefTest.ViewModels
{
  public class QuestionSetDetailsViewModel
  {
    public long Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string QuestionType { get; set; }
    public string Language { get; set; }
    public string Difficulty { get; set; }
    public long QuestionCount { get; set; }
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
    public long ExamId { get; set; }
  }

  public class QuestionSetStatusViewModel
  {
    public long Id { get; set; }
    public string Name { get; set; }
    public QuestionSetStatus Status { get; set; }
    public string StatusDescription { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public long QuestionsGenerated { get; set; }
  }

  public class QuestionViewModel
  {
    public long Id { get; set; }
    public long Index { get; set; }
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
    public long Id { get; set; }
    public long Index { get; set; }
    public string Text { get; set; }
    public long IsCorrect { get; set; }
  }

  public class MatchingPairViewModel
  {
    public long Id { get; set; }
    public string LeftSide { get; set; }
    public string RightSide { get; set; }
    public long DisplayOrder { get; set; }
  }

  public class OrderingItemViewModel
  {
    public long Id { get; set; }
    public string Text { get; set; }
    public long CorrectOrder { get; set; }
    public long DisplayOrder { get; set; }
  }
}
