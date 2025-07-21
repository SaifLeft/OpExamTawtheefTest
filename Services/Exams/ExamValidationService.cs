using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;

namespace TawtheefTest.Services.Exams
{
  // IExamValidationService.cs
  public interface IExamValidationService
  {
    Task<(bool IsValid, string ErrorMessage)> ValidateExamForPublishingAsync(int examId);
    Task<bool> HasQuestionsAsync(int examId);
    Task<bool> HasCandidatesAsync(int examId);
    Task<bool> HasSufficientQuestionsAsync(int examId);
  }

  // ExamValidationService.cs
  public class ExamValidationService : IExamValidationService
  {
    private readonly ApplicationDbContext _context;

    public ExamValidationService(ApplicationDbContext context)
    {
      _context = context;
    }

    public async Task<(bool IsValid, string ErrorMessage)> ValidateExamForPublishingAsync(int examId)
    {
      if (!await HasQuestionsAsync(examId))
        return (false, "لا يمكن نشر الاختبار لأنه لا يحتوي على أسئلة");

      if (!await HasCandidatesAsync(examId))
        return (false, "لا يمكن نشر الاختبار لأنه لا يحتوي على متقدمين");

      if (!await HasSufficientQuestionsAsync(examId))
        return (false, "لا يمكن نشر الاختبار لأنه لا يحتوي على عدد كافٍ من الأسئلة");

      return (true, string.Empty);
    }

    public async Task<bool> HasQuestionsAsync(int examId)
    {
      var questionsCount = await _context.ExamQuestionSetMappings
          .Where(eqs => eqs.ExamId == examId)
          .SelectMany(eqs => eqs.QuestionSet.Questions)
          .CountAsync();

      return questionsCount > 0;
    }

    public async Task<bool> HasCandidatesAsync(int examId)
    {
      var exam = await _context.Exams.FirstOrDefaultAsync(e => e.Id == examId);
      if (exam == null) return false;

      var candidatesCount = await _context.Candidates
          .Where(c => c.JobId == exam.JobId && c.IsActive)
          .CountAsync();

      return candidatesCount > 0;
    }

    public async Task<bool> HasSufficientQuestionsAsync(int examId)
    {
      var exam = await _context.Exams
          .Include(e => e.ExamQuestionSetMappings)
              .ThenInclude(eqs => eqs.QuestionSet)
                  .ThenInclude(qs => qs.Questions)
          .FirstOrDefaultAsync(e => e.Id == examId);

      if (exam == null) return false;

      var totalQuestions = exam.ExamQuestionSetMappings
          .SelectMany(eqs => eqs.QuestionSet.Questions)
          .Count();

      return exam.TotalQuestionsPerCandidate <= totalQuestions;
    }
  }
}
