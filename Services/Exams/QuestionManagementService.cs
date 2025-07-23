using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using TawtheefTest.DTOs.ExamModels;
using TawtheefTest.ViewModels;

namespace TawtheefTest.Services.Exams
{
  // IQuestionManagementService.cs
  public interface IQuestionManagementService
  {
    Task<List<ExamQuestionDTO>> GetExamQuestionsAsync(int examId);
    Task<Question> CreateQuestionAsync(int examId, AddQuestionViewModel model);
    Task<bool> DeleteQuestionAsync(int questionId);
    Task<Question> GetQuestionWithDetailsAsync(int questionId);
  }

  // QuestionManagementService.cs
  public class QuestionManagementService : IQuestionManagementService
  {
    private readonly ApplicationDbContext _context;

    public QuestionManagementService(ApplicationDbContext context)
    {
      _context = context;
    }

    public async Task<List<ExamQuestionDTO>> GetExamQuestionsAsync(int examId)
    {
      var exam = await _context.Exams
          .Include(e => e.ExamQuestionSetMappings)
              .ThenInclude(eqs => eqs.QuestionSet)
                  .ThenInclude(qs => qs.Questions)
                      .ThenInclude(q => q.Options)
          .Include(e => e.ExamQuestionSetMappings)
              .ThenInclude(eqs => eqs.QuestionSet)
                  .ThenInclude(qs => qs.Questions)
                      .ThenInclude(q => q.MatchingPairs)
          .Include(e => e.ExamQuestionSetMappings)
              .ThenInclude(eqs => eqs.QuestionSet)
                  .ThenInclude(qs => qs.Questions)
                      .ThenInclude(q => q.OrderingItems)
          .FirstOrDefaultAsync(m => m.Id == examId);

      if (exam == null) return new List<ExamQuestionDTO>();

      var allQuestions = exam.ExamQuestionSetMappings
          .SelectMany(eqs => eqs.QuestionSet.Questions)
          .ToList();

      return allQuestions
          .OrderBy(q => q.Index)
          .Select(q => new ExamQuestionDTO
          {
            Id = q.Id,
            SequenceNumber = q.Index,
            QuestionText = q.QuestionText,
            QuestionType = q.QuestionType,
            Answer = q.Answer,
            TrueFalseAnswer = q.TrueFalseAnswer,
            InstructionText = q.InstructionText,
            CorrectlyOrdered = q.QuestionType.ToLower() == "ordering"
                  ? q.OrderingItems.OrderBy(o => o.CorrectOrder).Select(o => o.Text).ToList()
                  : null,
            ShuffledOrder = q.QuestionType.ToLower() == "ordering"
                  ? q.OrderingItems.OrderBy(o => o.DisplayOrder).Select(o => o.Text).ToList()
                  : null,
            MatchingPairs = q.QuestionType.ToLower() == "matching"
                  ? q.MatchingPairs.Select(m => new MatchingPairDTO
                  {
                    Left = m.LeftItem,
                    Right = m.RightItem,
                    Index = m.DisplayOrder
                  }).ToList()
                  : null,
            Options = q.Options?.Select(o => new QuestionOptionDTO
            {
              Id = o.Id,
              Text = o.Text,
              Index = o.Index,
              IsCorrect = o.IsCorrect
            }).ToList()
          })
          .ToList();
    }

    public async Task<Question> CreateQuestionAsync(int examId, AddQuestionViewModel model)
    {
      var exam = await _context.Exams
          .Include(e => e.ExamQuestionSetMappings)
              .ThenInclude(eqs => eqs.QuestionSet)
                  .ThenInclude(qs => qs.Questions)
          .FirstOrDefaultAsync(e => e.Id == examId);

      if (exam == null) return null;

      var question = new Question
      {
        QuestionSetId = model.QuestionSetId,
        QuestionText = model.QuestionText,
        QuestionType = model.QuestionType,
        Index = 0,
        DisplayOrder = 0,
        CreatedAt = DateTime.Now
      };

      if (model.QuestionType == "TF")
      {
        question.TrueFalseAnswer = model.TrueFalseAnswer;
      }
      else if (model.QuestionType == "ShortAnswer" || model.QuestionType == "FillInTheBlank")
      {
        question.Answer = model.Answer;
      }

      _context.Questions.Add(question);
      await _context.SaveChangesAsync();

      // Update question index
      var questionCount = exam.ExamQuestionSetMappings
          .SelectMany(eqs => eqs.QuestionSet.Questions)
          .Count();

      question.Index = questionCount;
      question.DisplayOrder = questionCount;
      await _context.SaveChangesAsync();

      // Add options for MCQ questions
      if (model.QuestionType == "MCQ" && model.Options != null && model.Options.Count > 0)
      {
        int optionIndex = 0;
        foreach (var optionText in model.Options)
        {
          var option = new Option
          {
            QuestionId = question.Id,
            Text = optionText,
            IsCorrect = optionIndex == model.CorrectOptionIndex,
            Index = optionIndex
          };
          _context.Options.Add(option);
          optionIndex++;
        }
        await _context.SaveChangesAsync();
      }

      return question;
    }

    public async Task<bool> DeleteQuestionAsync(int questionId)
    {
      var question = await _context.Questions
          .Include(q => q.QuestionSet)
              .ThenInclude(qs => qs.ExamQuestionSetMappings)
          .FirstOrDefaultAsync(q => q.Id == questionId);

      if (question == null) return false;

      _context.Questions.Remove(question);
      await _context.SaveChangesAsync();
      return true;
    }

    public async Task<Question> GetQuestionWithDetailsAsync(int questionId)
    {
      return await _context.Questions
          .Include(q => q.Options)
          .Include(q => q.MatchingPairs)
          .Include(q => q.OrderingItems)
          .Include(q => q.QuestionSet)
          .FirstOrDefaultAsync(q => q.Id == questionId);
    }
  }
}
