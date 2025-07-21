using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using TawtheefTest.Enums;

namespace TawtheefTest.Services
{
  // IAssignmentService.cs
  public interface IAssignmentService
  {
    Task<Assignment> GetAssignmentByIdAsync(int assignmentId, int candidateId);
    Task<Assignment> GetAssignmentWithDetailsAsync(int assignmentId, int candidateId);
    Task<Assignment> CreateAssignmentAsync(int candidateId, int examId);
    Task<Assignment> GetExistingAttemptAsync(int candidateId, int examId);
    Task<Assignment> GetCompletedAssignmentAsync(int candidateId, int examId);
    Task<bool> ValidateAssignmentAccessAsync(int assignmentId, int candidateId);
    Task<List<Assignment>> GetCandidateAssignmentsAsync(int candidateId);
    Task<Assignment> CompleteAssignmentAsync(int assignmentId, int candidateId);
  }

  // AssignmentService.cs
  public class AssignmentService : IAssignmentService
  {
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public AssignmentService(ApplicationDbContext context, INotificationService notificationService)
    {
      _context = context;
      _notificationService = notificationService;
    }

    public async Task<Assignment> GetAssignmentByIdAsync(int assignmentId, int candidateId)
    {
      return await _context.Assignments
          .Include(ce => ce.Exam)
          .FirstOrDefaultAsync(ce => ce.Id == assignmentId && ce.CandidateId == candidateId);
    }

    public async Task<Assignment> GetAssignmentWithDetailsAsync(int assignmentId, int candidateId)
    {
      return await _context.Assignments
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSetMappings)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
                          .ThenInclude(q => q.Options)
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSetMappings)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
                          .ThenInclude(q => q.MatchingPairs)
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSetMappings)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
                          .ThenInclude(q => q.OrderingItems)
          .Include(ce => ce.CandidateAnswers)
          .Include(ce => ce.Candidate)
              .ThenInclude(c => c.Job)
          .FirstOrDefaultAsync(ce => ce.Id == assignmentId && ce.CandidateId == candidateId);
    }

    public async Task<Assignment> CreateAssignmentAsync(int candidateId, int examId)
    {
      var exam = await _context.Exams.FindAsync(examId);
      if (exam == null) return null;

      var assignment = new Assignment
      {
        CandidateId = candidateId,
        ExamId = examId,
        StartTime = DateTime.UtcNow,
        Status = AssignmentStatus.InProgress.ToString(),
        CreatedAt = DateTime.UtcNow,
        TotalQuestions = exam.TotalQuestionsPerCandidate,
        CompletedQuestions = 0
      };

      _context.Assignments.Add(assignment);
      await _context.SaveChangesAsync();

      // Create notification
      await _notificationService.CreateNotificationAsync(
          candidateId,
          "تم بدء اختبار جديد",
          $"لقد بدأت اختبار {exam.Name} بنجاح. عدد الأسئلة: {exam.TotalQuestionsPerCandidate}، مدة الاختبار: {exam.Duration} دقيقة.",
          "info"
      );

      return assignment;
    }

    public async Task<Assignment> GetExistingAttemptAsync(int candidateId, int examId)
    {
      return await _context.Assignments
          .FirstOrDefaultAsync(ce => ce.CandidateId == candidateId &&
                                   ce.ExamId == examId &&
                                   ce.Status == AssignmentStatus.InProgress.ToString());
    }

    public async Task<Assignment> GetCompletedAssignmentAsync(int candidateId, int examId)
    {
      return await _context.Assignments
          .FirstOrDefaultAsync(ce => ce.CandidateId == candidateId &&
                                   ce.ExamId == examId &&
                                   ce.Status == AssignmentStatus.Completed.ToString());
    }

    public async Task<bool> ValidateAssignmentAccessAsync(int assignmentId, int candidateId)
    {
      return await _context.Assignments
          .AnyAsync(ce => ce.Id == assignmentId && ce.CandidateId == candidateId);
    }

    public async Task<List<Assignment>> GetCandidateAssignmentsAsync(int candidateId)
    {
      return await _context.Assignments
          .Where(ce => ce.CandidateId == candidateId)
          .Include(ce => ce.Exam)
          .OrderByDescending(ce => ce.StartTime)
          .ToListAsync();
    }

    public async Task<Assignment> CompleteAssignmentAsync(int assignmentId, int candidateId)
    {
      var assignment = await GetAssignmentWithDetailsAsync(assignmentId, candidateId);
      if (assignment == null || assignment.Status == AssignmentStatus.Completed.ToString())
        return assignment;

      // Calculate score
      var allQuestions = assignment.Exam.ExamQuestionSetMappings
          .SelectMany(eqs => eqs.QuestionSet.Questions)
          .ToList();

      var totalQuestions = assignment.TotalQuestions > 0 ? assignment.TotalQuestions : allQuestions.Count;
      var answeredQuestions = assignment.CandidateAnswers.Select(ca => ca.QuestionId).Distinct().Count();
      var correctAnswers = assignment.CandidateAnswers.Count(ca => ca.IsCorrect == true);
      var score = totalQuestions > 0 ? (decimal)correctAnswers / totalQuestions * 100 : 0;

      // Update assignment
      assignment.EndTime = DateTime.UtcNow;
      assignment.Score = Math.Round(score, 2);
      assignment.Status = AssignmentStatus.Completed.ToString();
      assignment.UpdatedAt = DateTime.UtcNow;
      assignment.CompletedQuestions = answeredQuestions;
      assignment.TotalQuestions = totalQuestions;

      _context.Update(assignment);
      await _context.SaveChangesAsync();



      return assignment;
    }
  }
}
