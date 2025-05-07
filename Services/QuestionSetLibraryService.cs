using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TawtheefTest.Data.Structure;
using TawtheefTest.DTOs;
using TawtheefTest.Enum;
using TawtheefTest.ViewModels;

namespace TawtheefTest.Services
{
  public class QuestionSetLibraryService : IQuestionSetLibraryService
  {
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IOpExamQuestionGenerationService _questionGenerationService;

    public QuestionSetLibraryService(
        ApplicationDbContext context,
        IMapper mapper,
        IOpExamQuestionGenerationService questionGenerationService)
    {
      _context = context;
      _mapper = mapper;
      _questionGenerationService = questionGenerationService;
    }

    public async Task<List<QuestionSetDto>> GetAllQuestionSetsAsync()
    {
      var questionSets = await _context.QuestionSets
          .Include(qs => qs.ExamQuestionSets)
              .ThenInclude(eqs => eqs.Exam)
          .OrderByDescending(qs => qs.CreatedAt)
          .ToListAsync();

      return _mapper.Map<List<QuestionSetDto>>(questionSets);
    }

    public async Task<QuestionSetDto> GetQuestionSetDetailsAsync(int id)
    {
      var questionSet = await _context.QuestionSets
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.Options)
          .Include(qs => qs.ExamQuestionSets)
              .ThenInclude(eqs => eqs.Exam)
          .Include(qs => qs.ContentSources)
          .FirstOrDefaultAsync(qs => qs.Id == id);

      if (questionSet == null)
      {
        return null;
      }

      var dto = _mapper.Map<QuestionSetDto>(questionSet);
      dto.Questions = _mapper.Map<List<QuestionDto>>(questionSet.Questions.OrderBy(q => q.Index).ToList());

      // حساب عدد الاختبارات التي تستخدم هذه المجموعة
      dto.UsageCount = questionSet.ExamQuestionSets.Count;

      // إضافة أسماء الاختبارات التي تستخدم هذه المجموعة
      dto.UsedInExams = questionSet.ExamQuestionSets
          .Select(eqs => eqs.Exam.Name)
          .ToList();

      return dto;
    }

    public async Task<int> CreateQuestionSetAsync(QuestionSetCreateViewModel model)
    {
      // إنشاء مجموعة أسئلة جديدة
      var questionSet = new QuestionSet
      {
        Name = model.Name,
        Description = model.Description,
        QuestionType = model.QuestionType,
        Language = "Arabic", // يمكن تغييرها لتصبح قابلة للتخصيص
        Difficulty = model.Difficulty,
        QuestionCount = model.QuestionCount,
        OptionsCount = model.OptionsCount,
        Status = QuestionSetStatus.Pending,
        CreatedAt = DateTime.UtcNow
      };

      _context.QuestionSets.Add(questionSet);
      await _context.SaveChangesAsync();

      // إنشاء مصدر محتوى للأسئلة
      var contentSource = new ContentSource
      {
        ContentSourceType = model.ContentSourceType,
        Content = model.Topic,
        QuestionSetId = questionSet.Id,
        CreatedAt = DateTime.UtcNow
      };

      _context.ContentSources.Add(contentSource);
      await _context.SaveChangesAsync();

      // توليد الأسئلة باستخدام OpExam
      await _questionGenerationService.GenerateQuestionsFromTopicAsync(
          questionSet.Id,
          model.Topic,
          model.QuestionType,
          model.QuestionCount,
          model.Difficulty);

      return questionSet.Id;
    }

    public async Task AddQuestionSetToExamAsync(int examId, int questionSetId, int displayOrder)
    {
      var exam = await _context.Exams.FindAsync(examId);
      var questionSet = await _context.QuestionSets.FindAsync(questionSetId);

      if (exam == null || questionSet == null)
      {
        throw new Exception("الاختبار أو مجموعة الأسئلة غير موجودة");
      }

      // تحقق مما إذا كانت مجموعة الأسئلة مضافة بالفعل للاختبار
      var existingLink = await _context.ExamQuestionSets
          .FirstOrDefaultAsync(eqs => eqs.ExamId == examId && eqs.QuestionSetId == questionSetId);

      if (existingLink != null)
      {
        // تحديث ترتيب العرض فقط إذا كانت مجموعة الأسئلة مضافة بالفعل
        existingLink.DisplayOrder = displayOrder;
      }
      else
      {
        // إنشاء ارتباط جديد بين الاختبار ومجموعة الأسئلة
        var examQuestionSet = new ExamQuestionSet
        {
          ExamId = examId,
          QuestionSetId = questionSetId,
          DisplayOrder = displayOrder
        };

        _context.ExamQuestionSets.Add(examQuestionSet);
      }

      await _context.SaveChangesAsync();
    }

    public async Task<int> CloneQuestionSetAsync(int questionSetId)
    {
      var originalQuestionSet = await _context.QuestionSets
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.Options)
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.MatchingPairs)
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.OrderingItems)
          .Include(qs => qs.ContentSources)
          .FirstOrDefaultAsync(qs => qs.Id == questionSetId);

      if (originalQuestionSet == null)
      {
        return 0;
      }

      // إنشاء نسخة من مجموعة الأسئلة
      var newQuestionSet = new QuestionSet
      {
        Name = $"{originalQuestionSet.Name} (نسخة)",
        Description = originalQuestionSet.Description,
        QuestionType = originalQuestionSet.QuestionType,
        Language = originalQuestionSet.Language,
        Difficulty = originalQuestionSet.Difficulty,
        QuestionCount = originalQuestionSet.QuestionCount,
        OptionsCount = originalQuestionSet.OptionsCount,
        NumberOfRows = originalQuestionSet.NumberOfRows,
        NumberOfCorrectOptions = originalQuestionSet.NumberOfCorrectOptions,
        Status = QuestionSetStatus.Completed, // تعيين الحالة كمكتملة لأن الأسئلة ستكون جاهزة
        CreatedAt = DateTime.UtcNow
      };

      _context.QuestionSets.Add(newQuestionSet);
      await _context.SaveChangesAsync();

      // نسخ مصادر المحتوى
      foreach (var contentSource in originalQuestionSet.ContentSources)
      {
        var newContentSource = new ContentSource
        {
          ContentSourceType = contentSource.ContentSourceType,
          Content = contentSource.Content,
          Url = contentSource.Url,
          QuestionSetId = newQuestionSet.Id,
          CreatedAt = DateTime.UtcNow
        };

        _context.ContentSources.Add(newContentSource);
      }

      await _context.SaveChangesAsync();

      // نسخ الأسئلة
      foreach (var question in originalQuestionSet.Questions)
      {
        var newQuestion = new Question
        {
          QuestionText = question.QuestionText,
          QuestionType = question.QuestionType,
          QuestionSetId = newQuestionSet.Id,
          Index = question.Index,
          CreatedAt = DateTime.UtcNow
        };

        _context.Questions.Add(newQuestion);
        await _context.SaveChangesAsync();

        // نسخ الخيارات إذا كانت موجودة
        if (question.Options != null && question.Options.Any())
        {
          foreach (var option in question.Options)
          {
            var newOption = new QuestionOption
            {
              Text = option.Text,
              IsCorrect = option.IsCorrect,
              QuestionId = newQuestion.Id,
              Index = option.Index
            };

            _context.Add(newOption);
          }
        }

        // نسخ أزواج المطابقة إذا كانت موجودة
        if (question.MatchingPairs != null && question.MatchingPairs.Any())
        {
          foreach (var pair in question.MatchingPairs)
          {
            var newPair = new MatchingPair
            {
              LeftItem = pair.LeftItem,
              RightItem = pair.RightItem,
              QuestionId = newQuestion.Id,
              DisplayOrder = pair.DisplayOrder
            };

            _context.MatchingPairs.Add(newPair);
          }
        }

        // نسخ عناصر الترتيب إذا كانت موجودة
        if (question.OrderingItems != null && question.OrderingItems.Any())
        {
          foreach (var item in question.OrderingItems)
          {
            var newItem = new OrderingItem
            {
              Text = item.Text,
              CorrectOrder = item.CorrectOrder,
              QuestionId = newQuestion.Id,
              DisplayOrder = item.DisplayOrder
            };

            _context.OrderingItems.Add(newItem);
          }
        }
      }

      await _context.SaveChangesAsync();
      return newQuestionSet.Id;
    }

    public async Task ShuffleQuestionOptionsAsync(int questionSetId, ShuffleType shuffleType)
    {
      var questionSet = await _context.QuestionSets
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.Options)
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.OrderingItems)
          .FirstOrDefaultAsync(qs => qs.Id == questionSetId);

      if (questionSet == null)
      {
        throw new Exception("مجموعة الأسئلة غير موجودة");
      }

      var random = new Random();

      // خلط الأسئلة إذا تم تحديد ذلك
      if (shuffleType == ShuffleType.QuestionsOnly || shuffleType == ShuffleType.Both)
      {
        var questions = questionSet.Questions.ToList();
        for (int i = 0; i < questions.Count; i++)
        {
          questions[i].Index = i + 1;
        }

        // خلط ترتيب الأسئلة
        for (int i = questions.Count - 1; i > 0; i--)
        {
          int j = random.Next(0, i + 1);
          var temp = questions[i].Index;
          questions[i].Index = questions[j].Index;
          questions[j].Index = temp;
        }
      }

      // خلط الخيارات إذا تم تحديد ذلك
      if (shuffleType == ShuffleType.OptionsOnly || shuffleType == ShuffleType.Both)
      {
        foreach (var question in questionSet.Questions)
        {
          // خلط خيارات أسئلة الاختيار من متعدد
          if (question.Options != null && question.Options.Any())
          {
            var options = question.Options.ToList();
            for (int i = 0; i < options.Count; i++)
            {
              options[i].Index = i + 1;
            }

            // خلط ترتيب الخيارات
            for (int i = options.Count - 1; i > 0; i--)
            {
              int j = random.Next(0, i + 1);
              var temp = options[i].Index;
              options[i].Index = options[j].Index;
              options[j].Index = temp;
            }
          }

          // خلط عناصر أسئلة الترتيب
          if (question.OrderingItems != null && question.OrderingItems.Any())
          {
            var items = question.OrderingItems.ToList();

            // لا نغير الترتيب الصحيح، بل نغير فقط ترتيب العرض
            var displayOrder = Enumerable.Range(1, items.Count).ToList();
            // خلط ترتيب العرض
            for (int i = displayOrder.Count - 1; i > 0; i--)
            {
              int j = random.Next(0, i + 1);
              var temp = displayOrder[i];
              displayOrder[i] = displayOrder[j];
              displayOrder[j] = temp;
            }

            // تعيين ترتيب العرض الجديد (هذا سيكون مختلف عن الترتيب الصحيح)
            for (int i = 0; i < items.Count; i++)
            {
              // خلق قيمة DisplayOrder إذا لم تكن موجودة
              var item = items[i];
              item.DisplayOrder = displayOrder[i];
            }
          }
        }
      }

      await _context.SaveChangesAsync();
    }

    public async Task<List<QuestionSetDto>> GetReusableQuestionSetsAsync()
    {
      // الحصول على مجموعات الأسئلة المكتملة فقط
      var questionSets = await _context.QuestionSets
          .Where(qs => qs.Status == QuestionSetStatus.Completed)
          .Include(qs => qs.Questions)
          .OrderByDescending(qs => qs.CreatedAt)
          .ToListAsync();

      var dtos = _mapper.Map<List<QuestionSetDto>>(questionSets);

      // تعيين عدد الأسئلة لكل مجموعة
      for (int i = 0; i < questionSets.Count; i++)
      {
        dtos[i].QuestionCount = questionSets[i].Questions.Count;
      }

      return dtos;
    }
  }
}
