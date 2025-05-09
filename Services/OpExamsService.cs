using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using TawtheefTest.DTOs.ExamModels;
using AutoMapper;
using TawtheefTest.DTOs;
using TawtheefTest.Enum;

namespace TawtheefTest.Services
{
  public class OpExamsService : IOpExamsService
  {
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _serverUrl;
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IOpExamQuestionGenerationService _questionGenerationService;

    public OpExamsService(HttpClient httpClient, IConfiguration configuration, ApplicationDbContext context, IMapper mapper, IOpExamQuestionGenerationService questionGenerationService)
    {
      _httpClient = httpClient;
      _apiKey = configuration["OpExams:ApiKey"];
      _serverUrl = configuration["OpExams:ServerUrl"];
      _context = context;
      _mapper = mapper;
      _questionGenerationService = questionGenerationService;

      _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);
    }

    public async Task<List<ExamQuestionDTO>> GenerateQuestions(string topic, string questionType,
        string language, string difficulty, int questionCount, int optionsCount)
    {
      // Placeholder implementation - in a real implementation, would use data models internally
      // and map to DTOs when returning
      return new List<ExamQuestionDTO>();
    }

    public async Task<List<ExamQuestionDTO>> GenerateQuestionsFromText(string text, string questionType,
        string language, string difficulty, int questionCount, int optionsCount)
    {
      // Placeholder implementation
      return new List<ExamQuestionDTO>();
    }

    public async Task<List<ExamQuestionDTO>> GenerateQuestionsFromLink(string link, string questionType,
        string language, string difficulty, int questionCount, int optionsCount)
    {
      // Placeholder implementation
      return new List<ExamQuestionDTO>();
    }

    public async Task<List<ExamQuestionDTO>> GenerateQuestionsFromYoutube(string youtubeLink, string questionType,
        string language, string difficulty, int questionCount, int optionsCount)
    {
      // Placeholder implementation
      return new List<ExamQuestionDTO>();
    }

    public async Task<string> UploadFile(Stream fileStream, string fileName)
    {
      // Placeholder implementation
      return Guid.NewGuid().ToString();
    }

    public async Task<List<ExamQuestionDTO>> GenerateQuestionsFromDocument(string documentId, string questionType,
        string language, string difficulty, int questionCount, int optionsCount)
    {
      // Placeholder implementation
      return new List<ExamQuestionDTO>();
    }

    public async Task<List<ExamQuestionDTO>> GenerateQuestionsFromImage(string imageId, string questionType,
        string language, string difficulty, int questionCount, int optionsCount)
    {
      // Placeholder implementation
      return new List<ExamQuestionDTO>();
    }

    public async Task<List<ExamQuestionDTO>> GenerateQuestionsFromAudio(string audioId, string questionType,
        string language, string difficulty, int questionCount, int optionsCount)
    {
      // Placeholder implementation
      return new List<ExamQuestionDTO>();
    }

    public async Task<List<ExamQuestionDTO>> GenerateQuestionsFromVideo(string videoId, string questionType,
        string language, string difficulty, int questionCount, int optionsCount)
    {
      // Placeholder implementation
      return new List<ExamQuestionDTO>();
    }

    public async Task<List<ExamQuestionDTO>> GenerateQuestionsWithOrderingOrMatching(string topic, string questionType,
        string language, string difficulty, int questionCount, int numberOfRows)
    {
      // Placeholder implementation
      return new List<ExamQuestionDTO>();
    }

    public async Task<List<ExamQuestionDTO>> GenerateQuestionsWithMultiSelect(string topic, string questionType,
        string language, string difficulty, int questionCount, int optionsCount, int numberOfCorrectOptions)
    {
      // Placeholder implementation
      return new List<ExamQuestionDTO>();
    }

    public async Task GenerateQuestionsAsync(int questionSetId)
    {
      var questionSet = await _context.QuestionSets
          .Include(qs => qs.ContentSources)
          .FirstOrDefaultAsync(qs => qs.Id == questionSetId);

      if (questionSet == null)
      {
        throw new Exception($"مجموعة الأسئلة بالمعرف {questionSetId} غير موجودة");
      }

      if (questionSet.ContentSources == null || !questionSet.ContentSources.Any())
      {
        throw new Exception("لا توجد مصادر محتوى لمجموعة الأسئلة");
      }

      var contentSource = questionSet.ContentSources.First();
      string contentType = contentSource.ContentSourceType.ToLower();
      string content = contentSource.Content;

      try
      {
        questionSet.Status = QuestionSetStatus.Processing;
        await _context.SaveChangesAsync();

        switch (contentType)
        {
          case "topic":
            await _questionGenerationService.GenerateQuestionsFromTopicAsync(
                questionSetId, content, questionSet.QuestionType,
                questionSet.QuestionCount, questionSet.Difficulty);
            break;
          case "text":
            await _questionGenerationService.GenerateQuestionsFromTextAsync(
                questionSetId, content, questionSet.QuestionType,
                questionSet.QuestionCount, questionSet.Difficulty);
            break;
          case "link":
            await _questionGenerationService.GenerateQuestionsFromLinkAsync(
                questionSetId, content, questionSet.QuestionType,
                questionSet.QuestionCount, questionSet.Difficulty);
            break;
          case "youtube":
            await _questionGenerationService.GenerateQuestionsFromYoutubeAsync(
                questionSetId, contentSource.Url, questionSet.QuestionType,
                questionSet.QuestionCount, questionSet.Difficulty);
            break;
          case "document":
          case "pdf":
            await _questionGenerationService.GenerateQuestionsFromDocumentAsync(
                questionSetId, contentSource.UploadedFile.FileId, questionSet.QuestionType,
                questionSet.QuestionCount, questionSet.Difficulty);
            break;
          case "image":
            await _questionGenerationService.GenerateQuestionsFromImageAsync(
                questionSetId, content, questionSet.QuestionType,
                questionSet.QuestionCount, questionSet.Difficulty);
            break;
          case "audio":
            await _questionGenerationService.GenerateQuestionsFromAudioAsync(
                questionSetId, content, questionSet.QuestionType,
                questionSet.QuestionCount, questionSet.Difficulty);
            break;
          case "video":
            await _questionGenerationService.GenerateQuestionsFromVideoAsync(
                questionSetId, content, questionSet.QuestionType,
                questionSet.QuestionCount, questionSet.Difficulty);
            break;
          default:
            throw new Exception($"نوع المحتوى غير مدعوم: {contentType}");
        }
      }
      catch (Exception ex)
      {
        questionSet.Status = QuestionSetStatus.Failed;
        questionSet.ErrorMessage = ex.Message;
        await _context.SaveChangesAsync();
        throw;
      }
    }

    private string GetContentType(string fileName)
    {
      // Get content type based on file extension
      var extension = Path.GetExtension(fileName).ToLowerInvariant();

      return extension switch
      {
        ".pdf" => "application/pdf",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".doc" => "application/msword",
        ".jpg" => "image/jpeg",
        ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".mp3" => "audio/mpeg",
        ".mp4" => "video/mp4",
        ".wav" => "audio/wav",
        _ => "application/octet-stream", // Default content type
      };
    }
  }

  public class OpExamsResponse
  {
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("Questions")]
    public List<OpExamsQuestion> Questions { get; set; }
  }

  public class OpExamsQuestion
  {
    public int index { get; set; }
    public string Questions { get; set; }
    public List<string> Options { get; set; }
    public int answerIndex { get; set; }
    public string Answer { get; set; }
    public string id { get; set; }
  }
}
