using System;
using System.Collections.Generic;
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
using TawtheefTest.Models;

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
      _apiKey = _configuration["OpExam:ApiKey"];
      _baseUrl = _configuration["OpExam:BaseUrl"];
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
      HttpResponseMessage httpResponseMessage = default;
      try
      {
        var questionSet = await _dbContext.QuestionSets
            .FirstOrDefaultAsync(qs => qs.Id == questionSetId);

        if (questionSet == null)
        {
          throw new Exception($"مجموعة الأسئلة بالمعرف {questionSetId} غير موجودة");
        }

        // تحديث حالة مجموعة الأسئلة إلى "قيد المعالجة"
        questionSet.Status = QuestionSetStatus.Processing;
        await _dbContext.SaveChangesAsync();

        // إرسال الطلب إلى API
        var client = CreateHttpClient();

        var jsonContent = JsonSerializer.Serialize(request);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        httpResponseMessage = await client.PostAsync($"{_baseUrl}/questions-generator/v2", content);
        httpResponseMessage.EnsureSuccessStatusCode();

        var responseString = await httpResponseMessage.Content.ReadAsStringAsync();
        OpExamQuestionGenerationResponse opExamQuestionGenerationResponse = new OpExamQuestionGenerationResponse();
        OpExamTFQuestionResponse opExamTFQuestionResponse = new OpExamTFQuestionResponse();
        if (request.QuestionType == nameof(QuestionTypeEnum.TF))
        {
          opExamTFQuestionResponse = JsonSerializer.Deserialize<OpExamTFQuestionResponse>(responseString);
        }
        else
        {
          opExamQuestionGenerationResponse = JsonSerializer.Deserialize<OpExamQuestionGenerationResponse>(responseString);
        }

        // حفظ الأسئلة المُنشأة
        await SaveGeneratedQuestionsAsync(questionSet, opExamQuestionGenerationResponse, opExamTFQuestionResponse);

        // تحديث حالة مجموعة الأسئلة
        questionSet.Status = QuestionSetStatus.Completed;
        questionSet.ProcessedAt = DateTime.UtcNow;
        questionSet.QuestionCount = request.QuestionType == nameof(QuestionTypeEnum.TF)
            ? opExamTFQuestionResponse.Questions.Count
            : opExamQuestionGenerationResponse.Questions.Count;
        await _dbContext.SaveChangesAsync();

        return _mapper.Map<QuestionSetDto>(questionSet);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "خطأ في توليد الأسئلة لمجموعة الأسئلة بالمعرف: {QuestionSetId}", questionSetId);

        // تحديث حالة مجموعة الأسئلة إلى فشل
        var questionSet = await _dbContext.QuestionSets.FindAsync(questionSetId);
        if (questionSet != null)
        {
          questionSet.Status = QuestionSetStatus.Failed;

          var ErrorJsonFromApi = await httpResponseMessage.Content.ReadAsStringAsync();


          questionSet.ErrorMessage = ErrorJsonFromApi + " Error: " + ex.Message + " " + ex.InnerException?.Message;
          await _dbContext.SaveChangesAsync();
        }

        throw;
      }
    }

    private async Task SaveGeneratedQuestionsAsync(TawtheefTest.Data.Structure.QuestionSet questionSet, OpExamQuestionGenerationResponse response, OpExamTFQuestionResponse? tfResponse)
    {
      var questions = new List<Question>();

      if (questionSet.QuestionType.ToLower() == "tf" && tfResponse != null && tfResponse.Questions != null)
      {
        // معالجة أسئلة صح/خطأ من الاستجابة المخصصة
        foreach (var tfQuestion in tfResponse.Questions)
        {
          var question = new Question
          {
            QuestionSetId = questionSet.Id,
            QuestionText = tfQuestion.QuestionText,
            Index = tfQuestion.Index,
            QuestionType = questionSet.QuestionType,
            CreatedAt = DateTime.UtcNow,
            TrueFalseAnswer = tfQuestion.Answer
          };

          questions.Add(question);
        }
      }
      else if (response.Questions != null)
      {
        // معالجة باقي أنواع الأسئلة
        foreach (var opExamQuestion in response.Questions)
        {
          var question = new Question
          {
            QuestionSetId = questionSet.Id,
            QuestionText = opExamQuestion.QuestionText,
            Index = opExamQuestion.Index,
            QuestionType = questionSet.QuestionType,
            CreatedAt = DateTime.UtcNow,
            ExternalId = opExamQuestion.Id,
          };

          // معالجة الأسئلة حسب نوعها
          switch (questionSet.QuestionType.ToLower())
          {
            case "mcq":
              question.Options = CreateOptions(opExamQuestion.Options, opExamQuestion.AnswerIndex);
              if (opExamQuestion.AnswerIndex.HasValue)
              {
                question.Answer = opExamQuestion.Options[opExamQuestion.AnswerIndex.Value];
              }
              break;
            case "tf":
              question.TrueFalseAnswer = opExamQuestion.Answer?.ToLower() == "true";
              break;
            case "open":
              question.Answer = opExamQuestion.SampleAnswer ?? opExamQuestion.Answer;
              break;
            case "fillintheblank":
              question.Answer = opExamQuestion.Answer;
              break;
            case "ordering":
              question.InstructionText = opExamQuestion.InstructionText;
              question.OrderingItems = PrepareOrderItems(opExamQuestion.CorrectlyOrdered, opExamQuestion.ShuffledOrder);
              break;
            case "matching":
              var newMatchingPairs = new List<MatchingPair>();
              foreach (var pair in question.MatchingPairs)
              {
                newMatchingPairs.Add(new MatchingPair
                {
                  QuestionId = question.Id,
                  LeftItem = pair.LeftItem,
                  RightItem = pair.RightItem,
                  DisplayOrder = pair.DisplayOrder
                });
              }
              break;
            case "multiselect":
              question.Options = CreateOptions(opExamQuestion.Options, opExamQuestion.AnswerIndex);
              question.Answer = opExamQuestion.Answer;
              break;
            case "shortanswer":
              question.Answer = opExamQuestion.Answer;
              break;
          }

          questions.Add(question);
        }
      }

      await _dbContext.Questions.AddRangeAsync(questions);
      await _dbContext.SaveChangesAsync();

      //// نسخ خيارات الأسئلة إذا وجدت
      //if (questionSet.Questions != null && questionSet.Questions.Any())
      //{
      //  foreach (var question in questionSet.Questions)
      //  {
      //    if (question.Options != null && question.Options.Any())
      //    {
      //      var newOptions = new List<QuestionOption>();
      //      foreach (var option in question.Options)
      //      {
      //        newOptions.Add(new QuestionOption
      //        {
      //          QuestionId = question.Id,
      //          Text = option.Text,
      //          Index = option.Index,
      //          IsCorrect = option.IsCorrect
      //        });
      //      }
      //      _dbContext.QuestionOptions.AddRange(newOptions);
      //    }

      //    // نسخ بيانات الترتيب إذا كان السؤال من نوع الترتيب
      //    if (question.QuestionType.ToLower() == "ordering" && question.OrderingItems != null && question.OrderingItems.Any())
      //    {
      //      var newOrderingItems = new List<OrderingItem>();
      //      foreach (var item in question.OrderingItems)
      //      {
      //        newOrderingItems.Add(new OrderingItem
      //        {
      //          QuestionId = question.Id,
      //          Text = item.Text,
      //          CorrectOrder = item.CorrectOrder,
      //          DisplayOrder = item.DisplayOrder
      //        });
      //      }
      //      _dbContext.OrderingItems.AddRange(newOrderingItems);
      //    }

      //    // نسخ بيانات المطابقة إذا كان السؤال من نوع المطابقة
      //    if (question.QuestionType.ToLower() == "matching" && question.MatchingPairs != null && question.MatchingPairs.Any())
      //    {
      //      var newMatchingPairs = new List<MatchingPair>();
      //      foreach (var pair in question.MatchingPairs)
      //      {
      //        newMatchingPairs.Add(new MatchingPair
      //        {
      //          QuestionId = question.Id,
      //          LeftItem = pair.LeftItem,
      //          RightItem = pair.RightItem,
      //          DisplayOrder = pair.DisplayOrder
      //        });
      //      }
      //      _dbContext.MatchingPairs.AddRange(newMatchingPairs);
      //    }

      //    await _dbContext.SaveChangesAsync();
      //  }
      //}
    }

    private ICollection<OrderingItem> PrepareOrderItems(List<string> correctlyOrdered, List<string> shuffledOrder)
    {
      var result = new List<OrderingItem>();
      // Prepare Ordering Items Shuffled Order
      for (int i = 0; i < shuffledOrder.Count; i++)
      {
        result.Add(new OrderingItem
        {
          Text = shuffledOrder[i],
          DisplayOrder = i + 1
        });
      }
      // Prepare Ordering Items Correctly Ordered
      for (int i = 0; i < correctlyOrdered.Count; i++)
      {
        result.Add(new OrderingItem
        {
          Text = correctlyOrdered[i],
          CorrectOrder = i + 1,
        });
      }
      return result;
    }

    private List<QuestionOption> CreateOptions(List<string> options, int? answerIndex)
    {
      if (options == null) return new List<QuestionOption>();

      var result = new List<QuestionOption>();
      for (int i = 0; i < options.Count; i++)
      {
        result.Add(new QuestionOption
        {
          Text = options[i],
          Index = i + 1,
          IsCorrect = i == answerIndex
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

      if (questionSet == null || questionSet.Status != QuestionSetStatus.Completed)
      {
        return false;
      }

      var exam = await _dbContext.Exams.FindAsync(examId);
      if (exam == null)
      {
        return false;
      }

      // التحقق من وجود علاقة بين مجموعة الأسئلة والامتحان
      var examQuestionSet = await _dbContext.ExamQuestionSets
          .FirstOrDefaultAsync(eqs => eqs.ExamId == examId && eqs.QuestionSetId == questionSetId);

      if (examQuestionSet == null)
      {
        // إنشاء علاقة جديدة
        examQuestionSet = new TawtheefTest.Data.Structure.ExamQuestionSet
        {
          ExamId = examId,
          QuestionSetId = questionSetId,
          DisplayOrder = await _dbContext.ExamQuestionSets
              .Where(eqs => eqs.ExamId == examId)
              .CountAsync() + 1
        };

        _dbContext.ExamQuestionSets.Add(examQuestionSet);
        await _dbContext.SaveChangesAsync();
      }

      return true;
    }

    // جلب مجموعة الأسئلة من قاعدة البيانات
    private async Task<TawtheefTest.Data.Structure.QuestionSet> GetQuestionSetById(int questionSetId)
    {
      var questionSet = await _dbContext.QuestionSets
          .FirstOrDefaultAsync(qs => qs.Id == questionSetId);

      if (questionSet == null)
      {
        throw new Exception($"لم يتم العثور على مجموعة الأسئلة بالمعرف {questionSetId}");
      }

      return questionSet;
    }
  }
}
