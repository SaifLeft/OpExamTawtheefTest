using TawtheefTest.Data.Structure;

namespace TawtheefTest.Services
{
  // IExamCalculationService.cs
  public interface IExamCalculationService
  {
    TimeSpan? CalculateRemainingTime(Assignment assignment);
    int CalculateProgressPercentage(Assignment assignment, List<CandidateAnswer> candidateAnswers);
    decimal CalculateScore(Assignment assignment);
  }

  // ExamCalculationService.cs
  public class ExamCalculationService : IExamCalculationService
  {
    public TimeSpan? CalculateRemainingTime(Assignment assignment)
    {
      if (assignment.StartTime.HasValue || assignment.Exam == null)
        return null;

      DateTime endTime = assignment.StartTime.Value.AddMinutes(assignment.Exam.Duration);
      TimeSpan remaining = endTime - DateTime.UtcNow;

      return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
    }

    public int CalculateProgressPercentage(Assignment assignment, List<CandidateAnswer> candidateAnswers)
    {
      if (assignment.Exam?.ExamQuestionSetMappings == null ||
          !assignment.Exam.ExamQuestionSetMappings.Any() ||
          !assignment.Exam.ExamQuestionSetMappings.Any(eqs => eqs.QuestionSet?.Questions?.Any() == true))
      {
        return 0;
      }

      int totalQuestions = assignment.TotalQuestions > 0 ? assignment.TotalQuestions :
          assignment.Exam.ExamQuestionSetMappings.SelectMany(eqs => eqs.QuestionSet.Questions).Count();

      int answeredQuestions = candidateAnswers.Select(ca => ca.QuestionId).Distinct().Count();

      return totalQuestions > 0 ? (int)Math.Round((double)answeredQuestions / totalQuestions * 100) : 0;
    }

    public decimal CalculateScore(Assignment assignment)
    {
      if (assignment.Exam?.ExamQuestionSetMappings == null || !assignment.CandidateAnswers.Any())
        return 0;

      var totalQuestions = assignment.TotalQuestions > 0 ? assignment.TotalQuestions :
          assignment.Exam.ExamQuestionSetMappings.SelectMany(eqs => eqs.QuestionSet.Questions).Count();

      var correctAnswers = assignment.CandidateAnswers.Count(ca => ca.IsCorrect == true);

      return totalQuestions > 0 ? (decimal)correctAnswers / totalQuestions * 100 : 0;
    }
  }
}
