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

    public OpExamsService(HttpClient httpClient, IConfiguration configuration, ApplicationDbContext context, IMapper mapper)
    {
      _httpClient = httpClient;
      _apiKey = configuration["OpExams:ApiKey"];
      _serverUrl = configuration["OpExams:ServerUrl"];
      _context = context;
      _mapper = mapper;

      _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);
    }

    public async Task<List<QuestionDTO>> GenerateQuestions(string topic, string questionType,
        string language, string difficulty, int questionCount, int optionsCount)
    {
      // Placeholder implementation - in a real implementation, would use data models internally
      // and map to DTOs when returning
      return new List<QuestionDTO>();
    }

    public async Task<List<QuestionDTO>> GenerateQuestionsFromText(string text, string questionType,
        string language, string difficulty, int questionCount, int optionsCount)
    {
      // Placeholder implementation
      return new List<QuestionDTO>();
    }

    public async Task<List<QuestionDTO>> GenerateQuestionsFromLink(string link, string questionType,
        string language, string difficulty, int questionCount, int optionsCount)
    {
      // Placeholder implementation
      return new List<QuestionDTO>();
    }

    public async Task<List<QuestionDTO>> GenerateQuestionsFromYoutube(string youtubeLink, string questionType,
        string language, string difficulty, int questionCount, int optionsCount)
    {
      // Placeholder implementation
      return new List<QuestionDTO>();
    }

    public async Task<string> UploadFile(Stream fileStream, string fileName)
    {
      // Placeholder implementation
      return Guid.NewGuid().ToString();
    }

    public async Task<List<QuestionDTO>> GenerateQuestionsFromDocument(string documentId, string questionType,
        string language, string difficulty, int questionCount, int optionsCount)
    {
      // Placeholder implementation
      return new List<QuestionDTO>();
    }

    public async Task<List<QuestionDTO>> GenerateQuestionsFromImage(string imageId, string questionType,
        string language, string difficulty, int questionCount, int optionsCount)
    {
      // Placeholder implementation
      return new List<QuestionDTO>();
    }

    public async Task<List<QuestionDTO>> GenerateQuestionsFromAudio(string audioId, string questionType,
        string language, string difficulty, int questionCount, int optionsCount)
    {
      // Placeholder implementation
      return new List<QuestionDTO>();
    }

    public async Task<List<QuestionDTO>> GenerateQuestionsFromVideo(string videoId, string questionType,
        string language, string difficulty, int questionCount, int optionsCount)
    {
      // Placeholder implementation
      return new List<QuestionDTO>();
    }

    public async Task<List<QuestionDTO>> GenerateQuestionsWithOrderingOrMatching(string topic, string questionType,
        string language, string difficulty, int questionCount, int numberOfRows)
    {
      // Placeholder implementation
      return new List<QuestionDTO>();
    }

    public async Task<List<QuestionDTO>> GenerateQuestionsWithMultiSelect(string topic, string questionType,
        string language, string difficulty, int questionCount, int optionsCount, int numberOfCorrectOptions)
    {
      // Placeholder implementation
      return new List<QuestionDTO>();
    }

    public async Task<bool> GenerateQuestionsAsync(int questionSetId)
    {
      // Get the question set details
      var questionSet = await _context.QuestionSets
          .Include(qs => qs.ContentSources)
          .Include(qs => qs.ExamQuestionSets)
          .FirstOrDefaultAsync(qs => qs.Id == questionSetId);

      if (questionSet == null)
      {
        return false;
      }

      // Update status to processing
      questionSet.Status = QuestionSetStatus.Processing;
      await _context.SaveChangesAsync();

      try
      {
        // Get the content source (assuming there's at least one)
        var contentSource = questionSet.ContentSources.FirstOrDefault();
        if (contentSource == null)
        {
          questionSet.Status = QuestionSetStatus.Failed;
          questionSet.ErrorMessage = "No content source found";
          await _context.SaveChangesAsync();
          return false;
        }

        // Create dummy questions (for demonstration purposes)
        for (int i = 0; i < questionSet.QuestionCount; i++)
        {
          // Always use Data.Structure models when working with EF Core
          var question = new Question
          {
            QuestionSetId = questionSet.Id,
            ExamId = questionSet.ExamQuestionSets.FirstOrDefault()?.ExamId ?? 0,
            Index = i + 1,
            QuestionText = $"Sample question {i + 1} from {contentSource.Content ?? "content"}",
            QuestionType = questionSet.QuestionType,
            CreatedAt = DateTime.UtcNow
          };

          _context.Questions.Add(question);

          // Add options for multiple choice questions
          if (questionSet.QuestionType == QuestionTypeEnum.MCQ.ToString() || questionSet.QuestionType == QuestionTypeEnum.TF.ToString())
          {
            int optionsCount = questionSet.OptionsCount ?? 4;
            for (int j = 0; j < optionsCount; j++)
            {
              var option = new QuestionOption
              {
                QuestionId = question.Id,
                Index = j + 1,
                Text = $"Option {j + 1}",
                IsCorrect = j == 0 // Make the first option correct
              };

              _context.QuestionOptions.Add(option);
            }
          }
        }

        await _context.SaveChangesAsync();

        // Update status to completed
        questionSet.Status = QuestionSetStatus.Completed;
        questionSet.ProcessedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
      }
      catch (Exception ex)
      {
        // Update status to failed
        questionSet.Status = QuestionSetStatus.Failed;
        questionSet.ErrorMessage = ex.Message;
        await _context.SaveChangesAsync();
        return false;
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
