using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using TawtheefTest.DTOs;
using TawtheefTest.Enums;

namespace TawtheefTest.Services
{
  // IAnswerService.cs
  public interface IAnswerService
  {
    Task<CandidateAnswer> SaveAnswerAsync(SaveAnswerDTO model, int candidateId);
    bool ValidateAnswer(Question question, SaveAnswerDTO model);
    Task<CandidateAnswer> GetCandidateAnswerAsync(int assignmentId, int questionId);
    Task<CandidateAnswer> FlagQuestionAsync(QuestionFlagDTO model, int candidateId);
  }

  // AnswerService.cs
  public class AnswerService : IAnswerService
  {
    private readonly ApplicationDbContext _context;

    public AnswerService(ApplicationDbContext context)
    {
      _context = context;
    }

    public async Task<CandidateAnswer> SaveAnswerAsync(SaveAnswerDTO model, int candidateId)
    {
      // Get assignment
      var assignment = await _context.Assignments
          .Include(ce => ce.Exam)
          .FirstOrDefaultAsync(ce => ce.Id == model.AssignmentId && ce.CandidateId == candidateId);

      if (assignment == null || assignment.Status == AssignmentStatus.Completed.ToString())
        return null;

      // Get question
      var question = await _context.Questions
          .Include(q => q.Options)
          .Include(q => q.MatchingPairs)
          .Include(q => q.OrderingItems)
          .FirstOrDefaultAsync(q => q.Id == model.QuestionId);

      if (question == null) return null;

      // Check if answer exists
      var existingAnswer = await _context.CandidateAnswers
          .FirstOrDefaultAsync(ca => ca.AssignmentId == model.AssignmentId && ca.QuestionId == model.QuestionId);

      bool isCorrect = ValidateAnswer(question, model);

      if (existingAnswer != null)
      {
        // Update existing answer
        existingAnswer.AnswerText = model.AnswerText;
        existingAnswer.IsCorrect = isCorrect;
        existingAnswer.UpdatedAt = DateTime.UtcNow;
        _context.Update(existingAnswer);
      }
      else
      {
        // Create new answer
        var candidateAnswer = new CandidateAnswer
        {
          AssignmentId = model.AssignmentId,
          QuestionId = model.QuestionId,
          AnswerText = model.AnswerText,
          IsCorrect = isCorrect,
          CreatedAt = DateTime.UtcNow,
        };

        _context.CandidateAnswers.Add(candidateAnswer);
        existingAnswer = candidateAnswer;

        // Update completed questions count
        assignment.CompletedQuestions = await _context.CandidateAnswers
            .Where(ca => ca.AssignmentId == model.AssignmentId)
            .Select(ca => ca.QuestionId)
            .Distinct()
            .CountAsync() + 1;

        _context.Update(assignment);
      }

      await _context.SaveChangesAsync();
      return existingAnswer;
    }

    public bool ValidateAnswer(Question question, SaveAnswerDTO model)
    {
      return question.QuestionType switch
      {
        nameof(QuestionTypeEnum.MCQ) => ValidateMCQAnswer(question, model),
        nameof(QuestionTypeEnum.TF) => ValidateTrueFalseAnswer(question, model),
        nameof(QuestionTypeEnum.ShortAnswer) => ValidateShortAnswer(question, model),
        nameof(QuestionTypeEnum.FillInTheBlank) => ValidateFillInTheBlankAnswer(question, model),
        nameof(QuestionTypeEnum.MultiSelect) => ValidateMultiSelectAnswer(question, model),
        nameof(QuestionTypeEnum.Matching) => ValidateMatchingAnswer(question, model),
        nameof(QuestionTypeEnum.Ordering) => ValidateOrderingAnswer(question, model),
        _ => false
      };
    }

    private bool ValidateMCQAnswer(Question question, SaveAnswerDTO model)
    {
      var selectedOption = question.Options.FirstOrDefault(o => o.Id.ToString() == model.AnswerText);
      return selectedOption?.IsCorrect == true;
    }

    private bool ValidateTrueFalseAnswer(Question question, SaveAnswerDTO model)
    {
      if (bool.TryParse(model.AnswerText, out bool boolAnswer))
      {
        return question.TrueFalseAnswer.HasValue && boolAnswer == question.TrueFalseAnswer;
      }
      return false;
    }

    private bool ValidateShortAnswer(Question question, SaveAnswerDTO model)
    {
      return string.Equals(question.Answer, model.AnswerText, StringComparison.OrdinalIgnoreCase);
    }

    private bool ValidateFillInTheBlankAnswer(Question question, SaveAnswerDTO model)
    {
      if (question.Options != null && question.Options.Any() && !string.IsNullOrEmpty(model.AnswerText))
      {
        var selectedOption = question.Options.FirstOrDefault(o => o.Id.ToString() == model.AnswerText);
        return selectedOption?.IsCorrect == true;
      }
      return string.Equals(question.Answer, model.AnswerText, StringComparison.OrdinalIgnoreCase);
    }

    private bool ValidateMultiSelectAnswer(Question question, SaveAnswerDTO model)
    {
      try
      {
        var selectedOptions = model.SelectedOptionsIds;
        var correctOptions = question.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToList();
        return selectedOptions?.OrderBy(x => x).SequenceEqual(correctOptions.OrderBy(x => x)) == true;
      }
      catch
      {
        return false;
      }
    }

    private bool ValidateMatchingAnswer(Question question, SaveAnswerDTO model)
    {
      try
      {
        var pairs = model.MatchingPairsIds;
        var correctPairs = question.MatchingPairs.Select(p => p.Id).ToList();
        return pairs?.OrderBy(x => x).SequenceEqual(correctPairs.OrderBy(x => x)) == true;
      }
      catch
      {
        return false;
      }
    }

    private bool ValidateOrderingAnswer(Question question, SaveAnswerDTO model)
    {
      try
      {
        var orderItems = model.OrderingItemsIds;
        if (orderItems?.Count > 0)
        {
          var correctOrder = question.OrderingItems
              .OrderBy(o => o.CorrectOrder)
              .Select(o => o.Id)
              .ToList();
          return orderItems.SequenceEqual(correctOrder);
        }
        return false;
      }
      catch
      {
        return false;
      }
    }

    public async Task<CandidateAnswer> GetCandidateAnswerAsync(int assignmentId, int questionId)
    {
      return await _context.CandidateAnswers
          .FirstOrDefaultAsync(ca => ca.AssignmentId == assignmentId && ca.QuestionId == questionId);
    }

    public async Task<CandidateAnswer> FlagQuestionAsync(QuestionFlagDTO model, int candidateId)
    {
      // Validate assignment access
      var assignment = await _context.Assignments
          .FirstOrDefaultAsync(ce => ce.Id == model.AssignmentId && ce.CandidateId == candidateId);

      if (assignment == null) return null;

      // Get or create candidate answer for flagging
      var candidateAnswer = await GetCandidateAnswerAsync(model.AssignmentId, model.QuestionId);

      if (candidateAnswer == null)
      {
        candidateAnswer = new CandidateAnswer
        {
          AssignmentId = model.AssignmentId,
          QuestionId = model.QuestionId,
          IsFlagged = model.IsFlagged,
          CreatedAt = DateTime.UtcNow
        };
        _context.CandidateAnswers.Add(candidateAnswer);
      }
      else
      {
        candidateAnswer.IsFlagged = model.IsFlagged;
        candidateAnswer.UpdatedAt = DateTime.UtcNow;
        _context.Update(candidateAnswer);
      }

      await _context.SaveChangesAsync();
      return candidateAnswer;
    }
  }
}
