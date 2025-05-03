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
using TawtheefTest.Enum;

namespace TawtheefTest.Services
{
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

    public async Task<string> UploadFileAsync(byte[] fileData, string fileName)
    {
      try
      {
        var client = CreateHttpClient();

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileData);
        content.Add(fileContent, "file", fileName);

        var response = await client.PostAsync($"{_baseUrl}/questions-generator/v2/upload-file", content);
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<FileUploadResponse>(responseString);

        return result.Url;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "خطأ في رفع الملف: {FileName}", fileName);
        throw;
      }
    }

    public async Task<QuestionSetDto> GenerateQuestionsFromTopicAsync(int questionSetId, string topic, string questionType,
        int numberOfQuestions, string difficulty, string language = "Arabic")
    {
      var request = CreateBaseRequest(questionType, numberOfQuestions, difficulty, language);
      request.SourceContent = new SourceContent
      {
        Type = "topic",
        Topic = topic
      };

      return await GenerateQuestionsAsync(questionSetId, request);
    }

    public async Task<QuestionSetDto> GenerateQuestionsFromTextAsync(int questionSetId, string text, string questionType,
        int numberOfQuestions, string difficulty, string language = "Arabic")
    {
      var request = CreateBaseRequest(questionType, numberOfQuestions, difficulty, language);
      request.SourceContent = new SourceContent
      {
        Type = "text",
        Text = text
      };

      return await GenerateQuestionsAsync(questionSetId, request);
    }

    public async Task<QuestionSetDto> GenerateQuestionsFromLinkAsync(int questionSetId, string link, string questionType,
        int numberOfQuestions, string difficulty, string language = "Arabic")
    {
      var request = CreateBaseRequest(questionType, numberOfQuestions, difficulty, language);
      request.SourceContent = new SourceContent
      {
        Type = "link",
        Link = link
      };

      return await GenerateQuestionsAsync(questionSetId, request);
    }

    public async Task<QuestionSetDto> GenerateQuestionsFromYoutubeAsync(int questionSetId, string youtubeLink, string questionType,
        int numberOfQuestions, string difficulty, string language = "Arabic")
    {
      var request = CreateBaseRequest(questionType, numberOfQuestions, difficulty, language);
      request.SourceContent = new SourceContent
      {
        Type = "youtube",
        Link = youtubeLink
      };

      return await GenerateQuestionsAsync(questionSetId, request);
    }

    public async Task<QuestionSetDto> GenerateQuestionsFromDocumentAsync(int questionSetId, string documentUrl, string questionType,
        int numberOfQuestions, string difficulty, string language = "Arabic")
    {
      var request = CreateBaseRequest(questionType, numberOfQuestions, difficulty, language);
      request.SourceContent = new SourceContent
      {
        Type = "pdf",
        Document = documentUrl
      };

      return await GenerateQuestionsAsync(questionSetId, request);
    }

    public async Task<QuestionSetDto> GenerateQuestionsFromImageAsync(int questionSetId, string imageUrl, string questionType,
        int numberOfQuestions, string difficulty, string language = "Arabic")
    {
      var request = CreateBaseRequest(questionType, numberOfQuestions, difficulty, language);
      request.SourceContent = new SourceContent
      {
        Type = "image",
        Image = imageUrl
      };

      return await GenerateQuestionsAsync(questionSetId, request);
    }

    public async Task<QuestionSetDto> GenerateQuestionsFromAudioAsync(int questionSetId, string audioUrl, string questionType,
        int numberOfQuestions, string difficulty, string language = "Arabic")
    {
      var request = CreateBaseRequest(questionType, numberOfQuestions, difficulty, language);
      request.SourceContent = new SourceContent
      {
        Type = "audio",
        Audio = audioUrl
      };

      return await GenerateQuestionsAsync(questionSetId, request);
    }

    public async Task<QuestionSetDto> GenerateQuestionsFromVideoAsync(int questionSetId, string videoUrl, string questionType,
        int numberOfQuestions, string difficulty, string language = "Arabic")
    {
      var request = CreateBaseRequest(questionType, numberOfQuestions, difficulty, language);
      request.SourceContent = new SourceContent
      {
        Type = "video",
        Video = videoUrl
      };

      return await GenerateQuestionsAsync(questionSetId, request);
    }

    private OpExamQuestionGenerationRequest CreateBaseRequest(string questionType, int numberOfQuestions, string difficulty, string language)
    {
      return new OpExamQuestionGenerationRequest
      {
        QuestionType = questionType,
        NumberOfQuestions = numberOfQuestions,
        Difficulty = difficulty,
        Language = language
      };
    }

    private async Task<QuestionSetDto> GenerateQuestionsAsync(int questionSetId, OpExamQuestionGenerationRequest request)
    {
      try
      {
        // تحديث حالة مجموعة الأسئلة
        var questionSet = await _dbContext.QuestionSets
            .Include(qs => qs.ContentSources)
            .FirstOrDefaultAsync(qs => qs.Id == questionSetId);

        if (questionSet == null)
        {
          throw new Exception($"مجموعة الأسئلة بالمعرف {questionSetId} غير موجودة");
        }

        questionSet.Status = QuestionSetStatus.Processing;
        await _dbContext.SaveChangesAsync();

        // إرسال الطلب إلى API
        var client = CreateHttpClient();

        var jsonContent = JsonSerializer.Serialize(request);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"{_baseUrl}/questions-generator/v2", content);
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<OpExamQuestionGenerationResponse>(responseString);

        // حفظ الأسئلة المُنشأة
        await SaveGeneratedQuestionsAsync(questionSet, result);

        // تحديث حالة مجموعة الأسئلة
        questionSet.Status = QuestionSetStatus.Completed;
        questionSet.ProcessedAt = DateTime.UtcNow;
        questionSet.QuestionCount = result.Questions.Count;
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
          questionSet.ErrorMessage = ex.Message;
          await _dbContext.SaveChangesAsync();
        }

        throw;
      }
    }

    private async Task SaveGeneratedQuestionsAsync(QuestionSet questionSet, OpExamQuestionGenerationResponse response)
    {
      var questions = new List<Question>();

      foreach (var opExamQuestion in response.Questions)
      {
        var question = new Question
        {
          QuestionSetId = questionSet.Id,
          QuestionText = opExamQuestion.QuestionText,
          Index = opExamQuestion.Index,
          QuestionType = questionSet.QuestionType,
          CreatedAt = DateTime.UtcNow,
          ExamId = questionSet.ExamId,

        };

        // معالجة الأسئلة حسب نوعها
        switch (questionSet.QuestionType.ToLower())
        {
          case "mcq":
            question.Options = CreateOptions(opExamQuestion.Options);
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
            question.Answer = JsonSerializer.Serialize(new
            {
              correctlyOrdered = opExamQuestion.CorrectlyOrdered,
              shuffledOrder = opExamQuestion.ShuffledOrder,
              instructionText = opExamQuestion.InstructionText
            });
            break;
          case "matching":
            // معالجة خاصة للمطابقة
            break;
          case "multiselect":
            question.Options = CreateOptions(opExamQuestion.Options);
            question.Answer = opExamQuestion.Answer;
            break;
          case "shortanswer":
            question.Answer = opExamQuestion.Answer;
            break;
        }

        questions.Add(question);
      }

      await _dbContext.Questions.AddRangeAsync(questions);
      await _dbContext.SaveChangesAsync();
    }

    private List<QuestionOption> CreateOptions(List<string> options)
    {
      if (options == null) return new List<QuestionOption>();

      var result = new List<QuestionOption>();
      for (int i = 0; i < options.Count; i++)
      {
        result.Add(new QuestionOption
        {
          Text = options[i],
          Index = i + 1,
          IsCorrect = false
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
          .Include(qs => qs.ContentSources)
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.Options)
          .Include(qs => qs.ExamQuestionSets)
              .ThenInclude(eqs => eqs.Exam)
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

    public async Task<bool> RetryQuestionGenerationAsync(int questionSetId)
    {
      var questionSet = await _dbContext.QuestionSets
          .Include(qs => qs.ContentSources)
          .Include(qs => qs.Questions)
          .FirstOrDefaultAsync(qs => qs.Id == questionSetId);

      if (questionSet == null || questionSet.Status != QuestionSetStatus.Failed)
      {
        return false;
      }

      // حذف الأسئلة الموجودة
      _dbContext.Questions.RemoveRange(questionSet.Questions);
      await _dbContext.SaveChangesAsync();

      // إعادة تعيين حالة مجموعة الأسئلة
      questionSet.Status = QuestionSetStatus.Pending;
      questionSet.ErrorMessage = null;
      await _dbContext.SaveChangesAsync();

      try
      {
        // الحصول على مصدر المحتوى
        var contentSource = questionSet.ContentSources.FirstOrDefault();
        if (contentSource == null)
        {
          throw new Exception("لا يوجد مصدر محتوى لمجموعة الأسئلة");
        }

        string contentType = contentSource.ContentSourceType.ToLower();
        string content = contentSource.Content ?? contentSource.Url;

        switch (contentType)
        {
          case "topic":
            await GenerateQuestionsFromTopicAsync(questionSetId, content, questionSet.QuestionType,
                questionSet.QuestionCount, questionSet.Difficulty);
            break;
          case "text":
            await GenerateQuestionsFromTextAsync(questionSetId, content, questionSet.QuestionType,
                questionSet.QuestionCount, questionSet.Difficulty);
            break;
          case "link":
            await GenerateQuestionsFromLinkAsync(questionSetId, content, questionSet.QuestionType,
                questionSet.QuestionCount, questionSet.Difficulty);
            break;
          case "youtube":
            await GenerateQuestionsFromYoutubeAsync(questionSetId, content, questionSet.QuestionType,
                questionSet.QuestionCount, questionSet.Difficulty);
            break;
          case "document":
          case "pdf":
            await GenerateQuestionsFromDocumentAsync(questionSetId, content, questionSet.QuestionType,
                questionSet.QuestionCount, questionSet.Difficulty);
            break;
          case "image":
            await GenerateQuestionsFromImageAsync(questionSetId, content, questionSet.QuestionType,
                questionSet.QuestionCount, questionSet.Difficulty);
            break;
          case "audio":
            await GenerateQuestionsFromAudioAsync(questionSetId, content, questionSet.QuestionType,
                questionSet.QuestionCount, questionSet.Difficulty);
            break;
          case "video":
            await GenerateQuestionsFromVideoAsync(questionSetId, content, questionSet.QuestionType,
                questionSet.QuestionCount, questionSet.Difficulty);
            break;
          default:
            throw new Exception($"نوع المحتوى غير مدعوم: {contentType}");
        }

        return true;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "خطأ في إعادة محاولة توليد الأسئلة: {QuestionSetId}", questionSetId);
        questionSet.Status = QuestionSetStatus.Failed;
        questionSet.ErrorMessage = ex.Message;
        await _dbContext.SaveChangesAsync();
        return false;
      }
    }

    public async Task<bool> AddQuestionsToExamAsync(int questionSetId, int examId)
    {
      var questionSet = await _dbContext.QuestionSets
          .Include(qs => qs.Questions)
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
        examQuestionSet = new ExamQuestionSet
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

      // نسخ الأسئلة من مجموعة الأسئلة إلى الامتحان
      if (questionSet.Questions != null && questionSet.Questions.Any())
      {
        // البحث عن أقصى رقم ترتيبي في الامتحان
        var maxIndex = await _dbContext.Questions
            .Where(q => q.ExamId == examId)
            .Select(q => q.Index)
            .DefaultIfEmpty(0)
            .MaxAsync();

        foreach (var question in questionSet.Questions)
        {
          var newQuestion = new Question
          {
            ExamId = examId,
            QuestionSetId = question.QuestionSetId,
            QuestionText = question.QuestionText,
            QuestionType = question.QuestionType,
            Index = ++maxIndex,
            Answer = question.Answer,
            TrueFalseAnswer = question.TrueFalseAnswer,
            CreatedAt = DateTime.UtcNow
          };

          _dbContext.Questions.Add(newQuestion);
          await _dbContext.SaveChangesAsync();

          // نسخ خيارات السؤال إذا وجدت
          if (question.Options != null && question.Options.Any())
          {
            foreach (var option in question.Options)
            {
              var newOption = new QuestionOption
              {
                QuestionId = newQuestion.Id,
                Text = option.Text,
                Index = option.Index,
                IsCorrect = option.IsCorrect
              };

              _dbContext.QuestionOptions.Add(newOption);
            }

            await _dbContext.SaveChangesAsync();
          }
        }
      }

      return true;
    }
  }
}
