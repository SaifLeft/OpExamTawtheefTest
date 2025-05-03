using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using TawtheefTest.Data.Structure;
using TawtheefTest.DTOs;
using TawtheefTest.Enum;

namespace TawtheefTest.Services
{
  public class QuestionGenerationService : IQuestionGenerationService
  {
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IOpExamsService _opExamsService;

    public QuestionGenerationService(
        ApplicationDbContext context,
        IMapper mapper,
        IOpExamsService opExamsService)
    {
      _context = context;
      _mapper = mapper;
      _opExamsService = opExamsService;
    }

    public async Task<int> CreateQuestionSetAsync(CreateQuestionSetDto model)
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
        OptionsCount = model.OptionsCount,
        NumberOfRows = model.NumberOfRows,
        NumberOfCorrectOptions = model.NumberOfCorrectOptions,
        Status = QuestionSetStatus.Pending,
        CreatedAt = DateTime.UtcNow
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

        _context.ExamQuestionSets.Add(examQuestionSet);
        await _context.SaveChangesAsync();
      }

      // إنشاء مصدر المحتوى حسب النوع المحدد
      ContentSource contentSource = null;

      if (!string.IsNullOrEmpty(model.Topic))
      {
        contentSource = new ContentSource
        {
          ContentSourceType = ContentSourceType.Topic.ToString(),
          Content = model.Topic,
          QuestionSetId = questionSet.Id,
          CreatedAt = DateTime.UtcNow
        };
      }
      else if (!string.IsNullOrEmpty(model.TextContent))
      {
        contentSource = new ContentSource
        {
          ContentSourceType = ContentSourceType.Text.ToString(),
          Content = model.TextContent,
          QuestionSetId = questionSet.Id,
          CreatedAt = DateTime.UtcNow
        };
      }
      else if (!string.IsNullOrEmpty(model.LinkUrl))
      {
        contentSource = new ContentSource
        {
          ContentSourceType = ContentSourceType.Link.ToString(),
          Url = model.LinkUrl,
          QuestionSetId = questionSet.Id,
          CreatedAt = DateTime.UtcNow
        };
      }
      else if (!string.IsNullOrEmpty(model.YoutubeUrl))
      {
        contentSource = new ContentSource
        {
          ContentSourceType = ContentSourceType.Youtube.ToString(),
          Url = model.YoutubeUrl,
          QuestionSetId = questionSet.Id,
          CreatedAt = DateTime.UtcNow
        };
      }
      else if (model.UploadedFileId.HasValue)
      {
        var uploadedFile = await _context.UploadedFiles.FindAsync(model.UploadedFileId.Value);
        if (uploadedFile != null)
        {
          string contentSourceType;

          // تحديد نوع المصدر بناءً على نوع الملف
          switch (uploadedFile.FileType)
          {
            case nameof(FileType.Document):
              contentSourceType = ContentSourceType.Document.ToString();
              break;
            case nameof(FileType.Image):
              contentSourceType = ContentSourceType.Image.ToString();
              break;
            case nameof(FileType.Audio):
              contentSourceType = ContentSourceType.Audio.ToString();
              break;
            case nameof(FileType.Video):
              contentSourceType = ContentSourceType.Video.ToString();
              break;
            default:
              contentSourceType = ContentSourceType.Document.ToString();
              break;
          }

          contentSource = new ContentSource
          {
            ContentSourceType = contentSourceType,
            UploadedFileId = uploadedFile.Id,
            QuestionSetId = questionSet.Id,
            CreatedAt = DateTime.UtcNow
          };
        }
      }

      if (contentSource != null)
      {
        _context.ContentSources.Add(contentSource);
        await _context.SaveChangesAsync();
      }

      return questionSet.Id;
    }

    public async Task<QuestionSetDto> GetQuestionSetStatusAsync(int questionSetId)
    {
      var questionSet = await _context.QuestionSets
          .Include(qs => qs.ContentSources)
              .ThenInclude(cs => cs.UploadedFile)
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
          .Include(qs => qs.ContentSources)
              .ThenInclude(cs => cs.UploadedFile)
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.Options)
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
      questionSet.Status = QuestionSetStatus.Pending;
      questionSet.ErrorMessage = null;
      questionSet.ProcessedAt = null;
      await _context.SaveChangesAsync();

      return true;
    }

    public async Task<bool> AddQuestionsToExamAsync(int questionSetId, int examId)
    {
      var questionSet = await _context.QuestionSets
          .Include(qs => qs.Questions)
          .FirstOrDefaultAsync(qs => qs.Id == questionSetId);

      if (questionSet == null || questionSet.Status != QuestionSetStatus.Completed)
      {
        return false;
      }

      var exam = await _context.Exams.FindAsync(examId);
      if (exam == null)
      {
        return false;
      }

      // التحقق مما إذا كانت مجموعة الأسئلة مرتبطة بالفعل بالاختبار
      var examQuestionSet = await _context.ExamQuestionSets
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

        _context.ExamQuestionSets.Add(examQuestionSet);
      }

      // تحديث ExamId للأسئلة
      foreach (var question in questionSet.Questions)
      {
        question.ExamId = examId;
      }

      await _context.SaveChangesAsync();
      return true;
    }

    private async Task<int> GetNextDisplayOrderAsync(int examId)
    {
      var maxOrder = await _context.ExamQuestionSets
          .Where(eqs => eqs.ExamId == examId)
          .Select(eqs => (int?)eqs.DisplayOrder)
          .MaxAsync() ?? 0;

      return maxOrder + 1;
    }
  }
}
