using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TawtheefTest.Data.Structure;
using TawtheefTest.Enums;

namespace TawtheefTest.ViewModels
{
  // Base model classes for Views removed to avoid conflicts with existing definitions

  public class QuestionExamViewModel
  {
    public long Id { get; set; }
    public long QuestionSetId { get; set; }
    public long ExamId { get; set; }
    public long Index { get; set; }
    public string QuestionText { get; set; }
    public QuestionTypeEnum QuestionType { get; set; }
    public List<OptionViewModel> Options { get; set; }
    public long? AnswerIndex { get; set; }
    public string ExpectedAnswer { get; set; }
    public bool? TrueFalseAnswer { get; set; }
    public string CorrectAnswerExplanation { get; set; }
  }

  public class OptionViewModel
  {
    public long Id { get; set; }
    public long QuestionId { get; set; }
    public long Index { get; set; }
    public string Text { get; set; }
    public long IsCorrect { get; set; }
  }
}
