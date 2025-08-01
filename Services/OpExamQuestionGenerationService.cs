using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TawtheefTest.Data.Structure;
using TawtheefTest.DTOs;
using TawtheefTest.DTOs.OpExam;
using TawtheefTest.Enums;
using NodaTime;

namespace TawtheefTest.Services
{
  public interface IOpExamQuestionGenerationService
  {
    Task<string?> UploadFileAsync(byte[] fileData, string fileName);
    Task<QuestionSetDto> GenerateQuestionsAsync(int questionSetId, OpExamQuestionGenerationRequest request);
    Task<QuestionSetDto> GetQuestionSetDetailsAsync(int questionSetId);
    Task<QuestionSetDto> GetQuestionSetStatusAsync(int questionSetId);
    Task<bool> AddQuestionsToExamAsync(int questionSetId, int examId);
  }
  public class OpExamQuestionGenerationService : IOpExamQuestionGenerationService
  {
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpExamQuestionGenerationService> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public OpExamQuestionGenerationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OpExamQuestionGenerationService> logger,
        ApplicationDbContext dbContext,
        IMapper mapper)
    {
      _httpClientFactory = httpClientFactory;
      _configuration = configuration;
      _logger = logger;
      _dbContext = dbContext;
      _mapper = mapper;
      _apiKey = _configuration["OpExam:ApiKey"] ?? throw new ArgumentNullException("OpExam:ApiKey is not configured");
      _baseUrl = _configuration["OpExam:BaseUrl"] ?? throw new ArgumentNullException("OpExam:BaseUrl is not configured");
    }

    public async Task<string?> UploadFileAsync(byte[] fileData, string fileName)
    {
      try
      {
        var client = CreateHttpClient();

        // تحديد نوع الملف إذا كان معروفًا (مثلاً PDF)
        var fileContent = new ByteArrayContent(fileData);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

        var content = new MultipartFormDataContent();
        content.Add(fileContent, "file", fileName);

        var response = await client.PostAsync($"{_baseUrl}/questions-generator/v2/upload-file", content);
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<FileUploadResponse>(responseString);

        return result.Url;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"خطأ في رفع الملف: {fileName}");
        throw new Exception("حدث خطأ أثناء رفع الملف. الرجاء المحاولة مرة أخرى أو التواصل مع الدعم الفني.");
      }
    }

    public async Task<QuestionSetDto> GenerateQuestionsAsync(int questionSetId, OpExamQuestionGenerationRequest request)
    {
      // التحقق من صحة البيانات
      if (request == null)
        throw new ArgumentNullException(nameof(request), "طلب توليد الأسئلة مطلوب");

      var questionSet = await GetQuestionSetByIdAsync(questionSetId);

      try
      {
        // تحديث حالة مجموعة الأسئلة إلى "قيد المعالجة"
        await UpdateQuestionSetStatusAsync(questionSet, QuestionSetStatus.Processing);

        // إرسال الطلب إلى API وتلقي الاستجابة
        string responseString = await SendQuestionGenerationRequestAsync(request);
        //string responseString = await File.ReadAllTextAsync("Test_MultiSelect_Arabic_Medium_10_4_4_2_Topic.json");

        // حفظ الرسبون في سجل الأحداث
        _logger.LogInformation("استجابة API لتوليد الأسئلة: {Response}", responseString);
        if (string.IsNullOrWhiteSpace(responseString))
        {
          throw new InvalidOperationException("استجابة API فارغة أو غير صالحة");
        }

        await SaveResponseString(questionSet, responseString);


        // معالجة الاستجابة حسب نوع السؤال
        QuestionGenerationResult questionsData = await ProcessApiResponseAsync(responseString, request.QuestionType);

        // حفظ الأسئلة المُنشأة
        await SaveGeneratedQuestionsAsync(questionSet, questionsData);

        // تحديث حالة مجموعة الأسئلة إلى مكتملة
        await UpdateQuestionSetToCompletedAsync(questionSet, (int)questionsData.QuestionCount);

        return _mapper.Map<QuestionSetDto>(questionSet);
      }
      catch (HttpRequestException httpEx)
      {
        await HandleQuestionGenerationErrorAsync(questionSet, httpEx, "خطأ في الاتصال بـ API لتوليد الأسئلة");
        throw new InvalidOperationException("فشل في الاتصال بخدمة توليد الأسئلة. الرجاء المحاولة لاحقاً.", httpEx);
      }
      catch (JsonException jsonEx)
      {
        await HandleQuestionGenerationErrorAsync(questionSet, jsonEx, "خطأ في تحليل استجابة API");
        throw new InvalidOperationException("استجابة غير صحيحة من خدمة توليد الأسئلة.", jsonEx);
      }
      catch (Exception ex)
      {
        await HandleQuestionGenerationErrorAsync(questionSet, ex, $"خطأ عام في توليد الأسئلة لمجموعة الأسئلة بالمعرف: {questionSetId}");
        throw;
      }
    }

    private async Task SaveResponseString(QuestionSet questionSet, string responseString)
    {
      questionSet.ResponseString = responseString;
      questionSet.UpdatedAt = DateTime.Now;
      await _dbContext.SaveChangesAsync();
    }

    private async Task<QuestionSet> GetQuestionSetByIdAsync(int questionSetId)
    {
      var questionSet = await _dbContext.QuestionSets
          .FirstOrDefaultAsync(qs => qs.Id == questionSetId);

      if (questionSet == null)
        throw new ArgumentException($"مجموعة الأسئلة بالمعرف {questionSetId} غير موجودة", nameof(questionSetId));

      return questionSet;
    }

    private async Task UpdateQuestionSetStatusAsync(QuestionSet questionSet, QuestionSetStatus status)
    {
      questionSet.Status = status.ToString();
      questionSet.UpdatedAt = DateTime.Now;
      await _dbContext.SaveChangesAsync();
    }

    private async Task<string> SendQuestionGenerationRequestAsync(OpExamQuestionGenerationRequest request)
    {
      using var client = CreateHttpClient();
      var jsonContent = JsonSerializer.Serialize(request);
      var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

      var response = await client.PostAsync($"{_baseUrl}/questions-generator/v2", content);
      response.EnsureSuccessStatusCode();

      return await response.Content.ReadAsStringAsync();
    }

    private async Task<QuestionGenerationResult> ProcessApiResponseAsync(string responseString, string questionType)
    {
      if (string.IsNullOrWhiteSpace(responseString))
        throw new ArgumentException("استجابة API فارغة", nameof(responseString));

      QuestionTypeEnum questionTypeEnum;
      try
      {
        questionTypeEnum = Enum.Parse<QuestionTypeEnum>(questionType, true);
      }
      catch (ArgumentException)
      {
        throw new ArgumentException($"نوع السؤال '{questionType}' غير مدعوم", nameof(questionType));
      }

      return questionTypeEnum switch
      {
        QuestionTypeEnum.TF => await ProcessTrueFalseResponseAsync(responseString),
        QuestionTypeEnum.MultiSelect => await ProcessMultiSelectResponseAsync(responseString),
        _ => await ProcessStandardResponseAsync(responseString)
      };
    }

    private async Task<QuestionGenerationResult> ProcessTrueFalseResponseAsync(string responseString)
    {
      var response = JsonSerializer.Deserialize<OpExamTFQuestionResponse>(responseString);
      return new QuestionGenerationResult
      {
        TFResponse = response,
        QuestionCount = response?.Questions?.Count ?? 0
      };
    }

    private async Task<QuestionGenerationResult> ProcessMultiSelectResponseAsync(string responseString)
    {
      var response = JsonSerializer.Deserialize<OpExamMultiSelectQuestionResponse>(responseString);
      return new QuestionGenerationResult
      {
        MultiSelectResponse = response,
        QuestionCount = response?.Questions?.Count ?? 0
      };
    }

    private async Task<QuestionGenerationResult> ProcessStandardResponseAsync(string responseString)
    {
      var response = JsonSerializer.Deserialize<OpExamQuestionGenerationResponse>(responseString);
      return new QuestionGenerationResult
      {
        StandardResponse = response,
        QuestionCount = response?.Questions?.Count ?? 0
      };
    }


    private async Task UpdateQuestionSetToCompletedAsync(QuestionSet questionSet, int questionCount)
    {
      questionSet.Status = nameof(QuestionSetStatus.Completed);
      questionSet.ProcessedAt = DateTime.Now;
      questionSet.QuestionCount = questionCount;
      await _dbContext.SaveChangesAsync();
    }

    private async Task HandleQuestionGenerationErrorAsync(QuestionSet questionSet, Exception ex, string logMessage)
    {
      _logger.LogError(ex, logMessage);

      try
      {
        questionSet.Status = nameof(QuestionSetStatus.Failed);
        questionSet.ErrorMessage = $"خطأ: {ex.Message}";

        if (ex.InnerException != null)
          questionSet.ErrorMessage += $" - التفاصيل: {ex.InnerException.Message}";

        await _dbContext.SaveChangesAsync();
      }
      catch (Exception saveEx)
      {
        _logger.LogError(saveEx, "فشل في حفظ حالة الخطأ لمجموعة الأسئلة بالمعرف: {QuestionSetId}", questionSet.Id);
      }
    }

    // فئة مساعدة لتجميع نتائج معالجة API
    private class QuestionGenerationResult
    {
      public OpExamQuestionGenerationResponse? StandardResponse { get; set; }
      public OpExamTFQuestionResponse? TFResponse { get; set; }
      public OpExamMultiSelectQuestionResponse? MultiSelectResponse { get; set; }
      public int QuestionCount { get; set; }
    }

    private async Task SaveGeneratedQuestionsAsync(QuestionSet questionSet, QuestionGenerationResult questionsData)
    {
      var questions = new List<Question>();

      switch (questionSet.QuestionType.ToLower())
      {
        case "tf":
          questions = ProcessTrueFalseQuestions(questionSet, questionsData.TFResponse);
          break;
        case "multiselect":
          questions = ProcessMultiSelectQuestions(questionSet, questionsData.MultiSelectResponse);
          break;
        default:
          questions = ProcessStandardQuestions(questionSet, questionsData.StandardResponse);
          break;
      }

      if (questions.Any())
      {
        await _dbContext.Questions.AddRangeAsync(questions);
        await _dbContext.SaveChangesAsync();
      }
    }

    private List<Question> ProcessTrueFalseQuestions(QuestionSet questionSet, OpExamTFQuestionResponse? tfResponse)
    {
      var questions = new List<Question>();

      if (tfResponse?.Questions == null) return questions;

      foreach (var tfQuestion in tfResponse.Questions)
      {
        var question = new Question
        {
          QuestionSetId = questionSet.Id,
          QuestionText = tfQuestion.QuestionText,
          Index = tfQuestion.Index,
          QuestionType = questionSet.QuestionType,
          DifficultyLevel = questionSet.DifficultySet,
          TrueFalseAnswer = tfQuestion.Answer == 1,
          ExternalId = tfQuestion.Id,
          CreatedAt = DateTime.Now,
        };

        questions.Add(question);
      }

      return questions;
    }

    private List<Question> ProcessMultiSelectQuestions(QuestionSet questionSet, OpExamMultiSelectQuestionResponse? multiSelectResponse)
    {
      var questions = new List<Question>();

      if (multiSelectResponse?.Questions == null) return questions;

      foreach (var multiSelectQuestion in multiSelectResponse.Questions)
      {
        var question = new Question
        {
          ExternalId = multiSelectQuestion.Id,
          QuestionSetId = questionSet.Id,
          QuestionText = multiSelectQuestion.QuestionText,
          Index = multiSelectQuestion.Index,
          QuestionType = questionSet.QuestionType,
          DifficultyLevel = questionSet.DifficultySet,
          Options = CreateMultiSelectOptions(multiSelectQuestion.Options, multiSelectQuestion.Answer, multiSelectQuestion.AnswerIndexs),
          Answer = string.Join(",,,", multiSelectQuestion.Answer),
          AnswerIndexs = string.Join(",,,", multiSelectQuestion.AnswerIndexs),
          CreatedAt = DateTime.Now,
        };

        questions.Add(question);
      }

      return questions;
    }

    private List<Question> ProcessStandardQuestions(QuestionSet questionSet, OpExamQuestionGenerationResponse? standardResponse)
    {
      var questions = new List<Question>();

      if (standardResponse?.Questions == null) return questions;

      foreach (var opExamQuestion in standardResponse.Questions)
      {
        var question = CreateBaseQuestion(questionSet, opExamQuestion);
        ConfigureQuestionByType(question, opExamQuestion, questionSet.QuestionType);
        questions.Add(question);
      }

      return questions;
    }

    private Question CreateBaseQuestion(QuestionSet questionSet, OpExamQuestion opExamQuestion)
    {
      return new Question
      {
        QuestionSetId = questionSet.Id,
        QuestionText = opExamQuestion.QuestionText,
        Index = opExamQuestion.Index,
        QuestionType = questionSet.QuestionType,
        CreatedAt = DateTime.Now,
        ExternalId = opExamQuestion.Id,
        DifficultyLevel = questionSet.DifficultySet
      };
    }

    private void ConfigureQuestionByType(Question question, dynamic opExamQuestion, string questionType)
    {
      switch (questionType.ToLower())
      {
        case "mcq":
          question.Options = CreateOptions(opExamQuestion.Options, opExamQuestion.AnswerIndex);
          question.Answer = opExamQuestion.Options[opExamQuestion.AnswerIndex];
          _logger.LogInformation($"تم إنشاء سؤال MCQ: {question.QuestionText}, عدد الخيارات: {question.Options?.Count ?? 0}, الإجابة الصحيحة: {question.Answer}");
          break;

        case "tf":
          question.TrueFalseAnswer = opExamQuestion.Answer?.ToLower() == "true";
          break;

        case "open":
        case "shortanswer":
          question.Answer = opExamQuestion.Answer;
          question.SampleAnswer = opExamQuestion.SampleAnswer;
          _logger.LogInformation($"تم إنشاء سؤال إجابة قصيرة: {question.QuestionText}, الإجابة النموذجية: {question.Answer}");
          break;

        case "fillintheblank":
          // أسئلة ملء الفراغات قد تحتوي على خيارات للاختيار من بينها
          if (opExamQuestion.Options != null && opExamQuestion.Options.Count > 0)
          {
            question.Options = CreateOptions(opExamQuestion.Options, opExamQuestion.AnswerIndex);
          }
          question.Answer = opExamQuestion.Answer;
          _logger.LogInformation($"تم إنشاء سؤال ملء الفراغات: {question.QuestionText}, الإجابة: {question.Answer}, عدد الخيارات: {question.Options?.Count ?? 0}");
          break;

        case "ordering":
          question.InstructionText = opExamQuestion.InstructionText;
          question.QuestionText = opExamQuestion.QuestionText ?? question.InstructionText;
          question.OrderingItems = PrepareOrderItems(opExamQuestion.CorrectlyOrdered, opExamQuestion.ShuffledOrder);
          _logger.LogInformation($"تم إنشاء سؤال ترتيب: {question.QuestionText}, عدد العناصر: {question.OrderingItems?.Count ?? 0}");
          break;

        case "matching":
          question.InstructionText = opExamQuestion.InstructionText;
          question.MatchingPairs = PrepareMatchingPairs(opExamQuestion.CorrectlyOrdered, opExamQuestion.ShuffledOrder);
          break;

        case "multiselect":
          question.Options = CreateMultiSelectOptions(opExamQuestion.Options, opExamQuestion.Answer, opExamQuestion.AnswerIndex);
          break;
      }
    }

    private ICollection<MatchingPair> PrepareMatchingPairs(List<string> correctlyOrdered, List<string> shuffledOrder)
    {
      var result = new List<MatchingPair>();

      if (correctlyOrdered == null || shuffledOrder == null)
        return result;

      // إنشاء أزواج المطابقة حيث العمود الأيسر هو الترتيب المخلوط والعمود الأيمن هو الترتيب الصحيح
      var minCount = Math.Min(correctlyOrdered.Count, shuffledOrder.Count);

      for (int i = 0; i < minCount; i++)
      {
        result.Add(new MatchingPair
        {
          LeftItem = shuffledOrder[i],     // العمود الأيسر (مخلوط)
          RightItem = correctlyOrdered[i], // العمود الأيمن (صحيح)
          DisplayOrder = i + 1
        });
      }

      return result;
    }

    private ICollection<OrderingItem> PrepareOrderItems(List<string> correctlyOrdered, List<string> shuffledOrder)
    {
      var result = new List<OrderingItem>();

      if (correctlyOrdered == null || shuffledOrder == null)
        return result;

      // إنشاء عناصر الترتيب حيث كل عنصر له ترتيب عرض وترتيب صحيح
      for (int i = 0; i < shuffledOrder.Count; i++)
      {
        result.Add(new OrderingItem
        {
          Text = shuffledOrder[i],
          CorrectOrder = 0,
          DisplayOrder = i + 1
        });
      }

      for (int i = 0; i < correctlyOrdered.Count; i++)
      {
        result.Add(new OrderingItem
        {
          Text = correctlyOrdered[i],
          CorrectOrder = i + 1,
          DisplayOrder = 0
        });
      }

      return result;
    }

    private List<Option> CreateOptions(List<string> options, int? answerIndex = null, List<long>? answerIndexs = null)
    {
      if (options == null) return new List<Option>();

      var result = new List<Option>();
      for (int i = 0; i < options.Count; i++)
      {
        bool isCorrect = false;

        // التحقق من الإجابة الصحيحة بناءً على answerIndex (للأسئلة أحادية الإجابة)
        if (answerIndex.HasValue)
        {
          isCorrect = answerIndex.Value == i;
        }
        // التحقق من الإجابات الصحيحة بناءً على answerIndexs (للأسئلة متعددة الإجابات)
        else if (answerIndexs != null)
        {
          isCorrect = answerIndexs.Contains(i);
        }

        result.Add(new Option
        {
          Text = options[i],
          Index = i + 1,
          IsCorrect = isCorrect,
        });
      }

      return result;
    }
    private List<Option> CreateMultiSelectOptions(List<string> options, List<string> Answers, List<int> answerIndexs)
    {
      if (options == null) return new List<Option>();

      var result = new List<Option>();
      for (int i = 0; i < options.Count; i++)
      {
        bool isCorrect = answerIndexs.Contains(i);


        result.Add(new Option
        {
          Text = options[i],
          Index = i + 1,
          IsCorrect = isCorrect,
        });
      }

      return result;
    }

    private HttpClient CreateHttpClient()
    {
      var client = _httpClientFactory.CreateClient();
      client.DefaultRequestHeaders.Add("api-key", _apiKey);
      return client;
    }

    public async Task<QuestionSetDto> GetQuestionSetDetailsAsync(int questionSetId)
    {
      var questionSet = await _dbContext.QuestionSets
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.Options)
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.MatchingPairs)
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.OrderingItems)
          .FirstOrDefaultAsync(qs => qs.Id == questionSetId);

      if (questionSet == null)
      {
        return null;
      }

      return _mapper.Map<QuestionSetDto>(questionSet);
    }

    public async Task<QuestionSetDto> GetQuestionSetStatusAsync(int questionSetId)
    {
      var questionSet = await _dbContext.QuestionSets
          .FirstOrDefaultAsync(qs => qs.Id == questionSetId);

      if (questionSet == null)
      {
        return null;
      }

      return _mapper.Map<QuestionSetDto>(questionSet);
    }

    public async Task<bool> AddQuestionsToExamAsync(int questionSetId, int examId)
    {
      var questionSet = await _dbContext.QuestionSets
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.Options)
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.MatchingPairs)
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.OrderingItems)
          .FirstOrDefaultAsync(qs => qs.Id == questionSetId);

      if (questionSet == null || questionSet.Status != nameof(QuestionSetStatus.Completed))
      {
        return false;
      }

      var exam = await _dbContext.Exams.FindAsync(examId);
      if (exam == null)
      {
        return false;
      }

      // التحقق من وجود علاقة بين مجموعة الأسئلة والامتحان
      var examQuestionSet = await _dbContext.ExamQuestionSetMappings
          .FirstOrDefaultAsync(eqs => eqs.ExamId == examId && eqs.QuestionSetId == questionSetId);

      if (examQuestionSet == null)
      {
        // إنشاء علاقة جديدة
        examQuestionSet = new ExamQuestionSetMapping
        {
          ExamId = examId,
          QuestionSetId = questionSetId,
          DisplayOrder = await _dbContext.ExamQuestionSetMappings
              .Where(eqs => eqs.ExamId == examId)
              .CountAsync() + 1
        };

        _dbContext.ExamQuestionSetMappings.Add(examQuestionSet);
        await _dbContext.SaveChangesAsync();
      }

      return true;
    }
  }
}
