using ITAM.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using TawtheefTest.DTOs;
using TawtheefTest.Enums;
using TawtheefTest.ViewModels;

namespace TawtheefTest.Services.Exams
{
  // IQuestionSetService.cs
  public interface IQuestionSetService
  {
    Task<bool> AssignQuestionSetsToExamAsync(int examId, List<int> questionSetIds);
    Task<List<QuestionSet>> GetQuestionSetsByExamAsync(int examId);
    Task<List<QuestionSet>> GetAvailableQuestionSetsAsync();

    // New methods for QuestionSetsController
    Task<QuestionSet> GetQuestionSetWithDetailsAsync(int id);
    Task<bool> CanDeleteQuestionSetAsync(int id);
    Task<bool> DeleteQuestionSetAsync(int id);
    Task<QuestionSetStatus> GetQuestionSetStatusAsync(int id);
    Task<AddQuestionSetToExamViewModel> PrepareAddToExamViewModelAsync(int questionSetId);
    Task<bool> RemoveQuestionSetFromExamAsync(int examId, int questionSetId);
    Task<int> CloneQuestionSetAsync(int id);
    Task<(bool Success, string ErrorMessage)> ValidateQuestionSetForMergeAsync(List<int> selectedIds, MergeQuestionSetsViewModel model);
    Task<int> MergeQuestionSetsAsync(MergeQuestionSetsViewModel model);
    Task<FileDownloadResult> GetQuestionSetFileAsync(int id);

    Task<(bool Success, string FileName, string ErrorMessage)> SaveFileAsync(IFormFile file, string contentSourceType);
    Task<IActionResult> DownloadFileAsync(int questionSetId, IFileMangmanent fileManager);
  }

  // QuestionSetService.cs
  public class QuestionSetService : IQuestionSetService
  {
    private readonly ApplicationDbContext _context;
    private readonly IQuestionSetLibraryService _libraryService;

    public QuestionSetService(ApplicationDbContext context, IQuestionSetLibraryService libraryService)
    {
      _context = context;
      _libraryService = libraryService;
    }
    public async Task<(bool Success, string FileName, string ErrorMessage)> SaveFileAsync(IFormFile file, string contentSourceType)
    {
      try
      {
        if (file?.Length > 0)
        {
          ContentSourceType contentSourceTypeEnum = (ContentSourceType)Enum.Parse(typeof(ContentSourceType), contentSourceType, true);
          // Note: This would need to be injected properly
          // For now, returning a placeholder
          return (true, $"file_{DateTime.Now:yyyyMMddHHmmss}_{file.FileName}", string.Empty);
        }
        return (false, string.Empty, "ملف غير صالح");
      }
      catch (Exception ex)
      {
        return (false, string.Empty, $"حدث خطأ أثناء حفظ الملف: {ex.Message}");
      }
    }

    public async Task<IActionResult> DownloadFileAsync(int questionSetId, IFileMangmanent fileManager)
    {
      var questionSet = await _context.QuestionSets
          .FirstOrDefaultAsync(qs => qs.Id == questionSetId);

      if (questionSet == null || string.IsNullOrEmpty(questionSet.FileName))
        return new NotFoundResult();

      try
      {
        var contentSourceType = Enum.Parse<ContentSourceType>(questionSet.ContentSourceType, true);
        var fileData = fileManager.GetFileByName(questionSet.FileName, contentSourceType);

        if (fileData == null || fileData.FileBytes == null || fileData.FileBytes.Length == 0)
          return new NotFoundResult();

        return new FileContentResult(fileData.FileBytes, fileData.FileContentType)
        {
          FileDownloadName = fileData.FileName
        };
      }
      catch (Exception)
      {
        return new NotFoundResult();
      }
    }
    public async Task<bool> AssignQuestionSetsToExamAsync(int examId, List<int> questionSetIds)
    {
      var exam = await _context.Exams
          .Include(e => e.ExamQuestionSetMappings)
          .FirstOrDefaultAsync(e => e.Id == examId);

      if (exam == null) return false;

      // Remove existing question set mappings
      _context.ExamQuestionSetMappings.RemoveRange(exam.ExamQuestionSetMappings);

      // Add new question set mappings
      int displayOrder = 1;
      foreach (var questionSetId in questionSetIds)
      {
        var examQuestionSet = new ExamQuestionSetMapping
        {
          ExamId = examId,
          QuestionSetId = questionSetId,
          DisplayOrder = displayOrder++
        };
        _context.ExamQuestionSetMappings.Add(examQuestionSet);
      }

      await _context.SaveChangesAsync();
      return true;
    }

    public async Task<List<QuestionSet>> GetQuestionSetsByExamAsync(int examId)
    {
      return await _context.QuestionSets
          .Where(qs => qs.ExamQuestionSetMappings.Any(eqs => eqs.ExamId == examId))
          .ToListAsync();
    }

    public async Task<List<QuestionSet>> GetAvailableQuestionSetsAsync()
    {
      return await _context.QuestionSets.ToListAsync();
    }

    public async Task<QuestionSet> GetQuestionSetWithDetailsAsync(int id)
    {
      return await _context.QuestionSets
          .Include(q => q.ExamQuestionSetMappings)
          .ThenInclude(e => e.Exam)
          .ThenInclude(e => e.Job)
          .Include(q => q.Questions)
          .FirstOrDefaultAsync(q => q.Id == id);
    }

    public async Task<bool> CanDeleteQuestionSetAsync(int id)
    {
      var questionSet = await _context.QuestionSets
          .Include(q => q.ExamQuestionSetMappings)
          .FirstOrDefaultAsync(q => q.Id == id);

      return questionSet != null && !questionSet.ExamQuestionSetMappings.Any();
    }

    public async Task<bool> DeleteQuestionSetAsync(int id)
    {
      var questionSet = await _context.QuestionSets
          .Include(q => q.ExamQuestionSetMappings)
          .FirstOrDefaultAsync(q => q.Id == id);

      if (questionSet == null || questionSet.ExamQuestionSetMappings.Any())
        return false;

      _context.QuestionSets.Remove(questionSet);
      await _context.SaveChangesAsync();
      return true;
    }

    public async Task<QuestionSetStatus> GetQuestionSetStatusAsync(int id)
    {
      var questionSet = await _context.QuestionSets.FindAsync(id);
      return questionSet == null || !Enum.TryParse<QuestionSetStatus>(questionSet.Status, true, out var status)
          ? QuestionSetStatus.Failed
          : status;
    }

    public async Task<AddQuestionSetToExamViewModel> PrepareAddToExamViewModelAsync(int questionSetId)
    {
      var questionSet = await GetQuestionSetWithDetailsAsync(questionSetId);
      if (questionSet == null || questionSet.Status != nameof(QuestionSetStatus.Completed))
        return null;

      var viewModel = new AddQuestionSetToExamViewModel
      {
        QuestionSetId = questionSet.Id,
        QuestionSetName = questionSet.Name,
        QuestionType = questionSet.QuestionType,
        Language = questionSet.Language == "Arabic" ? QuestionSetLanguage.Arabic : QuestionSetLanguage.English,
        QuestionCount = questionSet.QuestionCount,
        DisplayOrder = 1
      };

      // Currently assigned exams
      viewModel.AssignedExams = questionSet.ExamQuestionSetMappings
          .Select(e => new ExamSummaryViewModel
          {
            Id = e.Exam.Id,
            Name = e.Exam.Name,
            JobTitle = e.Exam.Job.Title,
            Status = (ExamStatus)Enum.Parse(typeof(ExamStatus), e.Exam.Status, true)
          })
          .ToList();

      // Available exams for assignment
      var assignedExamIds = viewModel.AssignedExams.Select(e => e.Id).ToList();
      var availableExams = await _context.Exams
          .Include(e => e.Job)
          .Where(e => !assignedExamIds.Contains(e.Id) && e.Status != nameof(ExamStatus.Archived))
          .ToListAsync();

      viewModel.AvailableExams = availableExams
          .Select(e => new ExamSummaryViewModel
          {
            Id = e.Id,
            Name = e.Name,
            JobTitle = e.Job.Title,
            Status = (ExamStatus)Enum.Parse(typeof(ExamStatus), e.Status, true)
          })
          .ToList();

      return viewModel;
    }

    public async Task<bool> RemoveQuestionSetFromExamAsync(int examId, int questionSetId)
    {
      await _libraryService.RemoveQuestionSetFromExam(examId, questionSetId);
      return true;
    }

    public async Task<int> CloneQuestionSetAsync(int id)
    {
      return await _libraryService.CloneQuestionSetAsync(id);
    }

    public async Task<(bool Success, string ErrorMessage)> ValidateQuestionSetForMergeAsync(List<int> selectedIds, MergeQuestionSetsViewModel model)
    {
      if (selectedIds == null || selectedIds.Count < 2)
        return (false, "يرجى اختيار مجموعتين أو أكثر للدمج.");

      if (string.IsNullOrWhiteSpace(model.MergedName))
        return (false, "يرجى إدخال اسم المجموعة الجديدة.");

      if (string.IsNullOrWhiteSpace(model.MergedType))
        return (false, "يرجى اختيار نوع الأسئلة للمجموعة الجديدة.");

      if (string.IsNullOrWhiteSpace(model.MergedDifficulty))
        return (false, "يرجى اختيار مستوى الصعوبة للمجموعة الجديدة.");

      if (string.IsNullOrWhiteSpace(model.MergedLanguage))
        return (false, "يرجى اختيار اللغة للمجموعة الجديدة.");

      // Check if question sets are compatible
      if (model.MergedType != "auto" && model.MergedType != "mixed")
      {
        var questionTypes = await _context.QuestionSets
            .Where(q => selectedIds.Contains(q.Id))
            .Select(q => q.QuestionType)
            .Distinct()
            .ToListAsync();

        if (questionTypes.Count > 1)
        {
          return (false, "تنبيه: المجموعات المختارة تحتوي على أنواع مختلفة من الأسئلة. هل تريد المتابعة رغم ذلك؟");
        }
      }

      return (true, string.Empty);
    }

    public async Task<int> MergeQuestionSetsAsync(MergeQuestionSetsViewModel model)
    {
      // Set default questions count if not specified
      if (model.QuestionsCountPerSet == null)
      {
        model.QuestionsCountPerSet = new Dictionary<int, int>();

        var questionSets = await _context.QuestionSets
            .Where(q => model.SelectedIds.Contains(q.Id))
            .Include(q => q.Questions)
            .ToListAsync();

        foreach (var set in questionSets)
        {
          model.QuestionsCountPerSet.Add(set.Id, set.Questions.Count);
        }
      }

      return await _libraryService.MergeSetsAsync(model);
    }

    public async Task<FileDownloadResult> GetQuestionSetFileAsync(int id)
    {
      var questionSet = await _context.QuestionSets
          .FirstOrDefaultAsync(qs => qs.Id == id);

      if (questionSet == null || string.IsNullOrEmpty(questionSet.FileName))
        return null;

      return new FileDownloadResult
      {
        QuestionSet = questionSet,
        ContentSourceType = Enum.Parse<ContentSourceType>(questionSet.ContentSourceType, true)
      };
    }
  }
}
