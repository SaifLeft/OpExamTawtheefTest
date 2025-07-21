using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using TawtheefTest.DTOs;
using TawtheefTest.DTOs.ExamModels;
using TawtheefTest.Enums;
using TawtheefTest.ViewModels;

namespace TawtheefTest.Services.Exams
{
  // IExamService.cs
  public interface IExamService
  {
    Task<List<ExamDto>> GetAllExamsAsync();
    Task<ExamDetailsDTO> GetExamDetailsAsync(int id);
    Task<Exam> GetExamByIdAsync(int id);
    Task<Exam> CreateExamAsync(CreateExamViewModel model);
    Task<Exam> UpdateExamAsync(int id, EditExamDTO examDto);
    Task<List<ExamListDTO>> GetExamsByJobAsync(int jobId);
    Task<List<ExamResultDto>> GetExamResultsAsync(int examId);
    Task<bool> ExamExistsAsync(int id);
    Task<bool> ToggleShowResultsAsync(int id);
    Task<bool> ToggleExamLinksAsync(int id);
    Task<bool> TogglePublishStatusAsync(int id);
  }

  // ExamService.cs
  public class ExamService : IExamService
  {
    private readonly ApplicationDbContext _context;

    public ExamService(ApplicationDbContext context)
    {
      _context = context;
    }

    public async Task<List<ExamDto>> GetAllExamsAsync()
    {
      return await _context.Exams
          .Include(e => e.Assignments)
          .Include(e => e.Job)
              .ThenInclude(e => e.Candidates)
          .Include(e => e.ExamQuestionSetMappings)
              .ThenInclude(eqs => eqs.QuestionSet)
          .Select(e => new ExamDto
          {
            Id = e.Id,
            Name = e.Name,
            JobId = e.JobId,
            JobName = e.Job.Title,
            Duration = e.Duration,
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            CreatedDate = e.CreatedAt,
            CandidatesCount = e.Job.Candidates.Count(),
            QuestionSets = e.ExamQuestionSetMappings.Select(eqs => new QuestionSetDto
            {
              Id = eqs.QuestionSet.Id,
              Name = eqs.QuestionSet.Name,
              Status = (QuestionSetStatus)Enum.Parse(typeof(QuestionSetStatus), eqs.QuestionSet.Status),
              QuestionCount = eqs.QuestionSet.QuestionCount
            }).ToList()
          })
          .ToListAsync();
    }

    public async Task<ExamDetailsDTO> GetExamDetailsAsync(int id)
    {
      var exam = await _context.Exams
          .Include(e => e.Job)
          .Include(e => e.ExamQuestionSetMappings)
              .ThenInclude(eqs => eqs.QuestionSet)
                  .ThenInclude(qs => qs.Questions)
          .Include(e => e.ExamQuestionSetMappings)
              .ThenInclude(eqs => eqs.QuestionSet)
                  .ThenInclude(qs => qs.Questions)
                      .ThenInclude(q => q.OptionChoices)
          .Include(e => e.ExamQuestionSetMappings)
              .ThenInclude(eqs => eqs.QuestionSet)
                  .ThenInclude(qs => qs.Questions)
                      .ThenInclude(q => q.MatchingPairs)
          .Include(e => e.ExamQuestionSetMappings)
              .ThenInclude(eqs => eqs.QuestionSet)
                  .ThenInclude(qs => qs.Questions)
                      .ThenInclude(q => q.OrderingItems)
          .Include(e => e.Assignments)
              .ThenInclude(ce => ce.Candidate)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (exam == null) return null;

      return new ExamDetailsDTO
      {
        Id = exam.Id,
        Name = exam.Name,
        JobId = exam.JobId,
        JobName = exam.Job.Title,
        Duration = exam.Duration,
        CreatedDate = exam.CreatedAt,
        ExamStartDate = exam.StartDate,
        ExamEndDate = exam.EndDate,
        TotalQuestionsPerCandidate = exam.TotalQuestionsPerCandidate,
        ShowResultsImmediately = exam.ShowResultsImmediately,
        SendExamLinkToApplicants = exam.SendExamLinkToApplicants,
        Status = (ExamStatus)Enum.Parse(typeof(ExamStatus), exam.Status),
        QuestionSets = exam.ExamQuestionSetMappings
              .OrderBy(eqs => eqs.DisplayOrder)
              .Select(eqs => new QuestionSetDto
              {
                Id = eqs.QuestionSet.Id,
                Name = eqs.QuestionSet.Name,
                Description = eqs.QuestionSet.Description,
                QuestionType = eqs.QuestionSet.QuestionType,
                QuestionCount = eqs.QuestionSet.QuestionCount,
                Status = (QuestionSetStatus)Enum.Parse(typeof(QuestionSetStatus), eqs.QuestionSet.Status),
                StatusDescription = GetStatusDescription((QuestionSetStatus)Enum.Parse(typeof(QuestionSetStatus), eqs.QuestionSet.Status)),
                ContentSourceType = eqs.QuestionSet.ContentSourceType,
                Difficulty = eqs.QuestionSet.DifficultySet,
                UpdatedAt = eqs.QuestionSet.UpdatedAt,
                CreatedAt = eqs.QuestionSet.CreatedAt
              }).ToList(),
        Candidates = exam.Assignments
              .Select(ce => new ExamCandidateDTO
              {
                Id = ce.Id,
                CandidateId = ce.CandidateId,
                Name = ce.Candidate.Name,
                StartTime = ce.StartTime,
                EndTime = ce.EndTime,
                Score = ce.Score,
                Status = Enum.GetValues<AssignmentStatus>().FirstOrDefault(s => s.ToString() == ce.Status.ToString()),
              }).ToList()
      };
    }

    public async Task<Exam> GetExamByIdAsync(int id)
    {
      return await _context.Exams.FindAsync(id);
    }

    public async Task<Exam> CreateExamAsync(CreateExamViewModel model)
    {
      var exam = new Exam
      {
        Name = model.Name,
        JobId = model.JobId,
        Duration = model.Duration,
        StartDate = model.ExamStartDate,
        EndDate = model.ExamEndDate,
        Status = nameof(ExamStatus.Draft),
        CreatedAt = DateTime.UtcNow,
        ShowResultsImmediately = model.ShowResultsImmediately,
        SendExamLinkToApplicants = model.SendExamLinkToApplicants
      };

      _context.Add(exam);
      await _context.SaveChangesAsync();
      return exam;
    }

    public async Task<Exam> UpdateExamAsync(int id, EditExamDTO examDto)
    {
      var exam = await _context.Exams.FindAsync(id);
      if (exam == null) return null;

      exam.Name = examDto.Name;
      exam.JobId = examDto.JobId;
      exam.Duration = examDto.Duration;
      exam.StartDate = examDto.StartDate;
      exam.EndDate = examDto.EndDate;
      exam.ShowResultsImmediately = examDto.ShowResultsImmediately;
      exam.SendExamLinkToApplicants = examDto.SendExamLinkToApplicants;
      exam.UpdatedAt = DateTime.UtcNow;

      _context.Update(exam);
      await _context.SaveChangesAsync();
      return exam;
    }

    public async Task<List<ExamListDTO>> GetExamsByJobAsync(int jobId)
    {
      return await _context.Exams
          .Where(e => e.JobId == jobId)
          .Include(e => e.Job)
          .Include(e => e.ExamQuestionSetMappings)
              .ThenInclude(eqs => eqs.QuestionSet)
                  .ThenInclude(qs => qs.Questions)
          .Select(e => new ExamListDTO
          {
            Id = e.Id,
            Name = e.Name,
            JobId = e.JobId,
            JobName = e.Job.Title,
            Duration = e.Duration,
            QuestionsCount = e.ExamQuestionSetMappings.Sum(eqs => eqs.QuestionSet.Questions.Count),
            CreatedDate = e.CreatedAt
          })
          .ToListAsync();
    }

    public async Task<List<ExamResultDto>> GetExamResultsAsync(int examId)
    {
      var exam = await _context.Exams
          .Include(e => e.Assignments)
              .ThenInclude(ce => ce.Candidate)
          .FirstOrDefaultAsync(m => m.Id == examId);

      if (exam == null) return new List<ExamResultDto>();

      return exam.Assignments.Select(ce => new ExamResultDto
      {
        Id = ce.Id,
        ExamId = ce.ExamId,
        ApplicantName = ce.Candidate.Name,
        StartTime = ce.StartTime,
        EndTime = ce.EndTime,
        Score = ce.Score,
        Status = ce.Status.ToString()
      }).ToList();
    }

    public async Task<bool> ExamExistsAsync(int id)
    {
      return await _context.Exams.AnyAsync(e => e.Id == id);
    }

    public async Task<bool> ToggleShowResultsAsync(int id)
    {
      var exam = await _context.Exams.FindAsync(id);
      if (exam == null) return false;

      exam.ShowResultsImmediately = !exam.ShowResultsImmediately;
      exam.UpdatedAt = DateTime.UtcNow;

      _context.Update(exam);
      await _context.SaveChangesAsync();
      return true;
    }

    public async Task<bool> ToggleExamLinksAsync(int id)
    {
      var exam = await _context.Exams.FindAsync(id);
      if (exam == null) return false;

      exam.SendExamLinkToApplicants = !exam.SendExamLinkToApplicants;
      exam.UpdatedAt = DateTime.UtcNow;

      _context.Update(exam);
      await _context.SaveChangesAsync();
      return true;
    }

    public async Task<bool> TogglePublishStatusAsync(int id)
    {
      var exam = await _context.Exams.FindAsync(id);
      if (exam == null) return false;

      exam.Status = exam.Status == nameof(ExamStatus.Published)
          ? nameof(ExamStatus.Draft)
          : nameof(ExamStatus.Published);

      exam.UpdatedAt = DateTime.UtcNow;
      _context.Update(exam);
      await _context.SaveChangesAsync();
      return true;
    }

    private string GetStatusDescription(QuestionSetStatus status)
    {
      return status switch
      {
        QuestionSetStatus.Pending => "في الانتظار",
        QuestionSetStatus.Processing => "قيد المعالجة",
        QuestionSetStatus.Completed => "مكتمل",
        QuestionSetStatus.Failed => "فشل",
        _ => status.ToString()
      };
    }
  }
}
