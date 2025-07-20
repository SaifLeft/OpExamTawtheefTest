using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using TawtheefTest.Data.Structure;
using TawtheefTest.DTOs;
using TawtheefTest.Enums;
using ITAM.Service;
using TawtheefTest.DTOs.Common;

namespace TawtheefTest.Services
{
  public interface IQuestionGenerationService
  {
    Task<bool> AddQuestionsToExamAsync(int questionSetId, int examId);
    Task<int> CreateQuestionSetAsync(CreateQuestionSetDto model);
    Task<QuestionSetDto> GetQuestionSetDetailsAsync(int questionSetId);
    Task<QuestionSetDto> GetQuestionSetStatusAsync(int questionSetId);
    Task<bool> RegenerateQuestions(int questionSetId);
    Task<bool> RetryQuestionGenerationAsync(int questionSetId);
  }

  public class QuestionGenerationService : IQuestionGenerationService
  {
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IFileMangmanent _file;

    public QuestionGenerationService(
        ApplicationDbContext context,
        IMapper mapper,
        IFileMangmanent file)
    {
      _context = context;
      _mapper = mapper;
      _file = file;
    }

    public async Task<long> CreateQuestionSetAsync(CreateQuestionSetDto model)
    {
      // إنشاء كيان مجموعة الأسئلة
      var questionSet = new QuestionSet
      {
        Name = model.Name,
        Description = model.Description,
        QuestionType = model.QuestionType.ToString(),
        Language = model.Language,
        Difficulty = model.Difficulty,
        QuestionCount = model.QuestionCount,
        OptionsCount = model.OptionsCount, // "optionsCount": number // available for multiple choice and true/false
        NumberOfRows = model.NumberOfRows, // "numberOfRows": number // available for matching and ordering
        NumberOfCorrectOptions = model.NumberOfCorrectOptions,
        Status = QuestionSetStatus.Pending.GetHashCode(),
        CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
        ContentSourceType = model.ContentSourceType,
        FileName = model.FileName,
        Content = model.TextContent ?? model.Topic,
        Url = model.LinkUrl ?? model.YoutubeUrl,
        ErrorMessage = null,
        ProcessedAt = null,
        UpdatedAt = null,

      };

      _context.QuestionSets.Add(questionSet);
      await _context.SaveChangesAsync();

      // ربط مجموعة الأسئلة بالاختبار إذا تم تحديد اختبار
      if (model.ExamId > 0)
      {
        var examQuestionSet = new ExamQuestionSet
        {
          ExamId = model.ExamId,
          QuestionSetId = questionSet.Id,
          DisplayOrder = await GetNextDisplayOrderAsync(model.ExamId)
        };

        _context.ExamQuestionSetManppings.Add(examQuestionSet);
        await _context.SaveChangesAsync();
      }


      return questionSet.Id;
    }

    public async Task<QuestionSetDto> GetQuestionSetStatusAsync(int questionSetId)
    {
      var questionSet = await _context.QuestionSets
          .FirstOrDefaultAsync(qs => qs.Id == questionSetId);

      if (questionSet == null)
      {
        return null;
      }

      return _mapper.Map<QuestionSetDto>(questionSet);
    }

    public async Task<QuestionSetDto> GetQuestionSetDetailsAsync(int questionSetId)
    {
      var questionSet = await _context.QuestionSets
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.QuestionOptions)
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.MatchingPairs)
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.OrderingItems)
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.OptionChoices)
          .FirstOrDefaultAsync(qs => qs.Id == questionSetId);

      if (questionSet == null)
      {
        return null;
      }

      return _mapper.Map<QuestionSetDto>(questionSet);
    }

    public async Task<bool> RetryQuestionGenerationAsync(int questionSetId)
    {
      var questionSet = await _context.QuestionSets
          .FirstOrDefaultAsync(qs => qs.Id == questionSetId);

      if (questionSet == null || questionSet.Status != QuestionSetStatus.Failed)
      {
        return false;
      }

      // حذف الأسئلة الموجودة إن وجدت
      var existingQuestions = await _context.Questions
          .Where(q => q.QuestionSetId == questionSetId)
          .ToListAsync();

      if (existingQuestions.Any())
      {
        _context.Questions.RemoveRange(existingQuestions);
        await _context.SaveChangesAsync();
      }

      // إعادة تعيين الحالة إلى "في الانتظار"
      questionSet.Status = QuestionSetStatus.Pending.GetHashCode();
      questionSet.ErrorMessage = null;
      questionSet.ProcessedAt = null;
      await _context.SaveChangesAsync();

      return true;
    }

    public async Task<bool> RegenerateQuestions(int questionSetId)
    {
      var questionSet = await _context.QuestionSets
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.QuestionOptions)
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.MatchingPairs)
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.OrderingItems)
          .FirstOrDefaultAsync(qs => qs.Id == questionSetId);

      if (questionSet == null)
      {
        return false;
      }

      // حذف الأسئلة الحالية
      _context.Questions.RemoveRange(questionSet.Questions);
      await _context.SaveChangesAsync();

      // تحديث حالة مجموعة الأسئلة
      questionSet.Status = QuestionSetStatus.Pending.GetHashCode();
      questionSet.ProcessedAt = null;
      questionSet.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
      await _context.SaveChangesAsync();


      return true;
    }

    public async Task<bool> AddQuestionsToExamAsync(int questionSetId, int examId)
    {
      var questionSet = await _context.QuestionSets
          .Include(qs => qs.Questions)
          .FirstOrDefaultAsync(qs => qs.Id == questionSetId);

      if (questionSet == null || questionSet.Status.GetHashCode() != QuestionSetStatus.Completed.GetHashCode())
      {
        return false;
      }

      var exam = await _context.Exams.FindAsync(examId);
      if (exam == null)
      {
        return false;
      }

      // التحقق مما إذا كانت مجموعة الأسئلة مرتبطة بالفعل بالاختبار
      var examQuestionSet = await _context.ExamQuestionSetManppings
          .FirstOrDefaultAsync(eqs => eqs.ExamId == examId && eqs.QuestionSetId == questionSetId);

      if (examQuestionSet == null)
      {
        // إضافة ارتباط جديد
        examQuestionSet = new ExamQuestionSet
        {
          ExamId = examId,
          QuestionSetId = questionSetId,
          DisplayOrder = await GetNextDisplayOrderAsync(examId)
        };

        _context.ExamQuestionSetManppings.Add(examQuestionSet);
      }


      await _context.SaveChangesAsync();
      return true;
    }

    private async Task<long> GetNextDisplayOrderAsync(int examId)
    {
      var maxOrder = await _context.ExamQuestionSetManppings
          .Where(eqs => eqs.ExamId == examId)
          .Select(eqs => (int?)eqs.DisplayOrder)
          .MaxAsync() ?? 0;

      return maxOrder + 1;
    }

  }
}
