using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;

namespace TawtheefTest.Services
{
  // IQuestionService.cs
  public interface IQuestionService
  {
    Task<List<Question>> GetQuestionsForAssignmentAsync(int assignmentId);
    Task<Question> GetNextUnansweredQuestionAsync(int assignmentId);
    Task<Question> FindReplacementQuestionAsync(int assignmentId, int questionToReplaceId);
    Task<Question> GetQuestionWithDetailsAsync(int questionId);
  }

  // QuestionService.cs
  public class QuestionService : IQuestionService
  {
    private readonly ApplicationDbContext _context;

    public QuestionService(ApplicationDbContext context)
    {
      _context = context;
    }

    public async Task<List<Question>> GetQuestionsForAssignmentAsync(int assignmentId)
    {
      var assignment = await _context.Assignments
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSetMappings)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
          .FirstOrDefaultAsync(ce => ce.Id == assignmentId);

      if (assignment == null) return new List<Question>();

      return assignment.Exam.ExamQuestionSetMappings
          .SelectMany(eqs => eqs.QuestionSet.Questions)
          .ToList();
    }

    public async Task<Question> GetNextUnansweredQuestionAsync(int assignmentId)
    {
      var assignment = await _context.Assignments
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSetMappings)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
          .Include(ce => ce.CandidateAnswers)
          .FirstOrDefaultAsync(ce => ce.Id == assignmentId);

      if (assignment == null) return null;

      var allQuestions = assignment.Exam.ExamQuestionSetMappings
          .SelectMany(eqs => eqs.QuestionSet.Questions)
          .ToList();

      var answeredQuestionIds = assignment.CandidateAnswers
          .Select(ca => ca.QuestionId)
          .ToHashSet();

      return allQuestions.FirstOrDefault(q => !answeredQuestionIds.Contains(q.Id));
    }

    public async Task<Question> FindReplacementQuestionAsync(int assignmentId, int questionToReplaceId)
    {
      // Get assignment and original question details
      var assignment = await _context.Assignments
          .Include(ce => ce.Exam)
          .FirstOrDefaultAsync(ce => ce.Id == assignmentId);

      if (assignment == null) return null;

      var originalQuestion = await _context.Questions
          .Include(q => q.QuestionSet)
          .FirstOrDefaultAsync(q => q.Id == questionToReplaceId);

      if (originalQuestion == null) return null;

      // Get question set IDs for this exam
      var questionSetIds = await _context.ExamQuestionSetMappings
          .Where(eqs => eqs.ExamId == assignment.ExamId)
          .Select(eqs => eqs.QuestionSetId)
          .ToListAsync();

      // Find replacement question of same type, excluding answered questions
      return await _context.Questions
          .Where(q => questionSetIds.Contains(q.QuestionSetId) &&
                     q.QuestionType == originalQuestion.QuestionType &&
                     q.Id != questionToReplaceId &&
                     !_context.CandidateAnswers.Any(ca => ca.AssignmentId == assignmentId && ca.QuestionId == q.Id))
          .OrderBy(q => Guid.NewGuid())
          .FirstOrDefaultAsync();
    }

    public async Task<Question> GetQuestionWithDetailsAsync(int questionId)
    {
      return await _context.Questions
          .Include(q => q.Options)
          .Include(q => q.MatchingPairs)
          .Include(q => q.OrderingItems)
          .FirstOrDefaultAsync(q => q.Id == questionId);
    }
  }
}
