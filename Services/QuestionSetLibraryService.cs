using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TawtheefTest.Data.Structure;
using TawtheefTest.DTOs;
using TawtheefTest.Enums;
using TawtheefTest.ViewModels;

namespace TawtheefTest.Services
{
  public interface IQuestionSetLibraryService
  {
    /// <summary>
    /// الحصول على جميع مجموعات الأسئلة
    /// </summary>
    Task<List<QuestionSetDto>> GetAllQuestionSetsAsync();

    /// <summary>
    /// الحصول على تفاصيل مجموعة أسئلة معينة
    /// </summary>
    Task<QuestionSetDto> GetQuestionSetDetailsAsync(int id);

    /// <summary>
    /// إنشاء مجموعة أسئلة جديدة
    /// </summary>
    Task<int> CreateQuestionSetAsync(QuestionSetCreateViewModel model);

    /// <summary>
    /// إضافة مجموعة أسئلة إلى اختبار
    /// </summary>
    Task AddQuestionSetToExam(AddToExamDTO DTO);


    /// <summary>
    /// نسخ مجموعة أسئلة
    /// </summary>
    Task<int> CloneQuestionSetAsync(int questionSetId);

    /// <summary>
    /// خلط خيارات الأسئلة في مجموعة أسئلة
    /// </summary>
    Task ShuffleQuestionOptionsAsync(int questionSetId, ShuffleType shuffleType);

    /// <summary>
    /// الحصول على مجموعات الأسئلة التي يمكن إعادة استخدامها
    /// </summary>
    Task<List<QuestionSetDto>> GetReusableQuestionSetsAsync();

    /// <summary>
    /// دمج عدة مجموعات أسئلة في مجموعة جديدة
    /// </summary>
    Task<int> MergeSetsAsync(MergeQuestionSetsViewModel model);

    /// <summary>
    /// البحث في مجموعات الأسئلة
    /// </summary>
    Task<List<QuestionSetDto>> SearchQuestionSetsAsync(string search = null, string questionType = null, string difficulty = null, string language = null);

    /// <summary>
    /// الحصول على مجموعة أسئلة معينة بواسطة المعرف
    /// </summary>
    Task<QuestionSetDto> GetQuestionSetByIdAsync(int id);

    /// <summary>
    /// حذف مجموعة أسئلة
    /// </summary>
    Task<bool> DeleteQuestionSetAsync(int id);
    Task RemoveQuestionSetFromExam(int examId, int questionSetId);
  }
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
          .Include(qs => qs.ExamQuestionSetManppings)
              .ThenInclude(eqs => eqs.Exam)
          .OrderByDescending(qs => qs.CreatedAt)
          .ToListAsync();

      return _mapper.Map<List<QuestionSetDto>>(questionSets);
    }

    public async Task<QuestionSetDto> GetQuestionSetDetailsAsync(int id)
    {
      var questionSet = await _context.QuestionSets
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.QuestionOptions)
          .Include(qs => qs.ExamQuestionSetManppings)
              .ThenInclude(eqs => eqs.Exam)
          .FirstOrDefaultAsync(qs => qs.Id == id);

      if (questionSet == null)
      {
        return null;
      }

      var dto = _mapper.Map<QuestionSetDto>(questionSet);
      dto.Questions = _mapper.Map<List<QuestionDto>>(questionSet.Questions.OrderBy(q => q.Index).ToList());

      // حساب عدد الاختبارات التي تستخدم هذه المجموعة
      dto.UsageCount = questionSet.ExamQuestionSetManppings.Count;

      // إضافة أسماء الاختبارات التي تستخدم هذه المجموعة
      dto.UsedInExams = questionSet.ExamQuestionSetManppings
          .Select(eqs => eqs.Exam.Name)
          .ToList();

      return dto;
    }

    public async Task<long> CreateQuestionSetAsync(QuestionSetCreateViewModel model)
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
        Status = QuestionSetStatus.Pending.GetHashCode(),
        CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
        ContentSourceType = model.ContentSourceType,
        Content = model.Topic,
      };

      _context.QuestionSets.Add(questionSet);
      await _context.SaveChangesAsync();



      return questionSet.Id;
    }

    public async Task AddQuestionSetToExam(AddToExamDTO DTO)
    {
      var exam = await _context.Exams.FindAsync(DTO.ExamId);
      var questionSet = await _context.QuestionSets.FindAsync(DTO.QuestionSetId);

      if (exam == null || questionSet == null)
      {
        throw new Exception("الاختبار أو مجموعة الأسئلة غير موجودة");
      }

      // تحقق مما إذا كانت مجموعة الأسئلة مضافة بالفعل للاختبار
      var existingLink = await _context.ExamQuestionSetManppings
          .FirstOrDefaultAsync(eqs => eqs.ExamId == DTO.ExamId && eqs.QuestionSetId == DTO.QuestionSetId);

      if (existingLink != null)
      {
        // تحديث ترتيب العرض فقط إذا كانت مجموعة الأسئلة مضافة بالفعل
        existingLink.DisplayOrder = DTO.DisplayOrder;
      }
      else
      {
        // إنشاء ارتباط جديد بين الاختبار ومجموعة الأسئلة
        var examQuestionSet = new ExamQuestionSet
        {
          ExamId = DTO.ExamId,
          QuestionSetId = DTO.QuestionSetId,
          DisplayOrder = DTO.DisplayOrder
        };

        _context.ExamQuestionSetManppings.Add(examQuestionSet);
      }

      await _context.SaveChangesAsync();
    }

    public async Task<int> CloneQuestionSetAsync(int questionSetId)
    {
      var originalQuestionSet = await _context.QuestionSets
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.QuestionOptions)
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.MatchingPairs)
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.OrderingItems)
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
        Status = QuestionSetStatus.Completed.GetHashCode(), // تعيين الحالة كمكتملة لأن الأسئلة ستكون جاهزة
        CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
      };

      _context.QuestionSets.Add(newQuestionSet);
      await _context.SaveChangesAsync();

      // نسخ محتوى المصدر مباشرة من QuestionSet الأصلي
      newQuestionSet.ContentSourceType = originalQuestionSet.ContentSourceType;
      newQuestionSet.Content = originalQuestionSet.Content;
      newQuestionSet.Url = originalQuestionSet.Url;
      newQuestionSet.FileName = originalQuestionSet.FileName;
      newQuestionSet.FileUploadedCode = originalQuestionSet.FileUploadedCode;

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
          CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };

        _context.Questions.Add(newQuestion);
        await _context.SaveChangesAsync();

        // نسخ الخيارات إذا كانت موجودة
        if (question.QuestionOptions != null && question.QuestionOptions.Any())
        {
          foreach (var option in question.QuestionOptions)
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
              .ThenInclude(q => q.QuestionOptions)
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
          if (question.QuestionOptions != null && question.QuestionOptions.Any())
          {
            var options = question.QuestionOptions.ToList();
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

    public async Task<int> MergeSetsAsync(MergeQuestionSetsViewModel model)
    {
      // جلب المجموعات المختارة والتأكد من أنها مكتملة
      var sets = await _context.QuestionSets
          .Where(q => model.SelectedIds.Contains(q.Id) && q.Status == QuestionSetStatus.Completed)
          .Include(q => q.Questions)
              .ThenInclude(q => q.Options)
          .Include(q => q.Questions)
              .ThenInclude(q => q.MatchingPairs)
          .Include(q => q.Questions)
              .ThenInclude(q => q.OrderingItems)
          .ToListAsync();

      if (sets.Count != model.SelectedIds.Count)
        throw new Exception("بعض المجموعات المختارة غير مكتملة أو غير موجودة.");

      // جلب الأسئلة من كل مجموعة حسب العدد المطلوب
      var mergedQuestions = new List<Question>();
      var setNames = new List<string>(); // لتجميع أسماء المجموعات للوصف

      foreach (var set in sets)
      {
        setNames.Add(set.Name);
        int count = model.QuestionsCountPerSet.ContainsKey(set.Id) ? model.QuestionsCountPerSet[set.Id] : set.Questions.Count;

        // تحقق من أن عدد الأسئلة المطلوب لا يتجاوز المتاح
        if (count > set.Questions.Count)
          count = set.Questions.Count;

        var questions = set.Questions.OrderBy(q => Guid.NewGuid()).Take(count).ToList();
        mergedQuestions.AddRange(questions);
      }

      // خلط الأسئلة إذا لزم الأمر
      if (model.ShuffleQuestions)
        mergedQuestions = mergedQuestions.OrderBy(q => Guid.NewGuid()).ToList();

      // إنشاء وصف للمجموعة الجديدة
      string mergedDescription = $"تم دمج هذه المجموعة من المجموعات التالية: {string.Join("، ", setNames)}";

      // إنشاء مجموعة جديدة
      var newSet = new QuestionSet
      {
        Name = model.MergedName,
        Description = mergedDescription,
        QuestionType = model.MergedType,
        Difficulty = model.MergedDifficulty,
        Language = model.MergedLanguage,
        Status = QuestionSetStatus.Completed.GetHashCode(),
        CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
        QuestionCount = mergedQuestions.Count,
        OptionsCount = sets.First().OptionsCount,
        NumberOfRows = sets.First().NumberOfRows,
        NumberOfCorrectOptions = sets.First().NumberOfCorrectOptions
      };

      _context.QuestionSets.Add(newSet);
      await _context.SaveChangesAsync();

      // نسخ الأسئلة وربطها بالمجموعة الجديدة
      int questionIndex = 0;
      foreach (var question in mergedQuestions)
      {
        var newQuestion = new Question
        {
          QuestionSetId = newSet.Id,
          QuestionText = question.QuestionText,
          QuestionType = question.QuestionType,
          Index = ++questionIndex, // ترتيب متسلسل جديد
          CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
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

            _context.QuestionOptions.Add(newOption);
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

      return newSet.Id;
    }

    public async Task<List<QuestionSetDto>> SearchQuestionSetsAsync(string search, string questionType, string difficulty, string language)
    {
      // بناء استعلام قاعدة البيانات
      var query = _context.QuestionSets
          .Include(qs => qs.ExamQuestionSetManppings)
              .ThenInclude(eqs => eqs.Exam)
          .Include(qs => qs.Questions)
          .AsQueryable();

      // إضافة شروط البحث
      if (!string.IsNullOrEmpty(search))
      {
        search = search.ToLower();
        query = query.Where(qs => qs.Name.ToLower().Contains(search) ||
                            (qs.Description != null && qs.Description.ToLower().Contains(search)));
      }

      if (!string.IsNullOrEmpty(questionType))
      {
        query = query.Where(qs => qs.QuestionType == questionType);
      }

      if (!string.IsNullOrEmpty(difficulty))
      {
        query = query.Where(qs => qs.Difficulty == difficulty);
      }

      if (!string.IsNullOrEmpty(language))
      {
        query = query.Where(qs => qs.Language == language);
      }

      // تنفيذ الاستعلام وترتيب النتائج
      var questionSets = await query
          .OrderByDescending(qs => qs.CreatedAt)
          .ToListAsync();

      // تحويل البيانات إلى DTOs
      var dtos = _mapper.Map<List<QuestionSetDto>>(questionSets);

      // إضافة معلومات إضافية لكل DTO
      for (int i = 0; i < questionSets.Count; i++)
      {
        // حساب عدد الأسئلة المولدة
        dtos[i].QuestionsGenerated = questionSets[i].Questions.Count;

        // إضافة أسماء الاختبارات المستخدمة فيها
        dtos[i].UsedInExams = questionSets[i].ExamQuestionSetManppings
            .Select(eqs => eqs.Exam.Name)
            .ToList();

        // حساب عدد الاستخدامات
        dtos[i].UsageCount = dtos[i].UsedInExams.Count;
      }

      return dtos;
    }

    public async Task<QuestionSetDto> GetQuestionSetByIdAsync(int id)
    {
      // البحث عن مجموعة الأسئلة بالمعرف
      var questionSet = await _context.QuestionSets
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.Options)
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.MatchingPairs)
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.OrderingItems)
          .Include(qs => qs.ExamQuestionSetManppings)
              .ThenInclude(eqs => eqs.Exam)
          .FirstOrDefaultAsync(qs => qs.Id == id);

      if (questionSet == null)
      {
        return new QuestionSetDto();
      }

      // تحويل البيانات إلى DTO
      var dto = _mapper.Map<QuestionSetDto>(questionSet);

      // ترتيب الأسئلة حسب الفهرس
      dto.Questions = _mapper.Map<List<QuestionDto>>(questionSet.Questions.OrderBy(q => q.Index).ToList());

      // إضافة معلومات إضافية للـ DTO
      dto.UsageCount = questionSet.ExamQuestionSetManppings.Count;
      dto.UsedInExams = questionSet.ExamQuestionSetManppings
          .Select(eqs => eqs.Exam.Name)
          .ToList();







      return dto;
    }

    public async Task<bool> DeleteQuestionSetAsync(int id)
    {
      // البحث عن مجموعة الأسئلة المراد حذفها
      var questionSet = await _context.QuestionSets
          .Include(qs => qs.ExamQuestionSetManppings)
          .FirstOrDefaultAsync(qs => qs.Id == id);

      if (questionSet == null)
      {
        return false;
      }

      // التحقق من أن مجموعة الأسئلة غير مستخدمة في أي اختبار
      if (questionSet.ExamQuestionSetManppings.Any())
      {
        throw new InvalidOperationException("لا يمكن حذف مجموعة الأسئلة لأنها مستخدمة في اختبارات. قم بإزالتها من الاختبارات أولاً.");
      }

      // حذف مجموعة الأسئلة
      _context.QuestionSets.Remove(questionSet);
      await _context.SaveChangesAsync();

      return true;
    }


    public Task RemoveQuestionSetFromExam(int examId, int questionSetId)
    {
      throw new NotImplementedException();
    }
  }
}
