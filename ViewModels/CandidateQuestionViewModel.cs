using System;
using System.Collections.Generic;
using TawtheefTest.Enums;

namespace TawtheefTest.ViewModels
{
  public class CandidateQuestionViewModel
  {
    public long Id { get; set; }
    public string QuestionType { get; set; }
    public string QuestionText { get; set; }
    public bool? TrueFalseAnswer { get; set; }
    public long DisplayOrder { get; set; }
    public string InstructionText { get; set; }
    public long IsAnswered { get; set; }
    public long IsFlagged { get; set; } = false;
    public List<QuestionOptionViewModel> Options { get; set; } = new List<QuestionOptionViewModel>();
    public List<OrderingItemViewModel> OrderingItems { get; set; } = new List<OrderingItemViewModel>();
    public List<MatchingPairViewModel> MatchingPairs { get; set; } = new List<MatchingPairViewModel>();
  }
}
