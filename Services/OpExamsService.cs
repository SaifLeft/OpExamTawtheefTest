using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TawtheefTest.Models;

namespace TawtheefTest.Services
{
  public class OpExamsService : IOpExamsService
  {
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _serverUrl;

    public OpExamsService(HttpClient httpClient, IConfiguration configuration)
    {
      _httpClient = httpClient;
      _apiKey = configuration["OpExams:ApiKey"];
      _serverUrl = configuration["OpExams:ServerUrl"];

      _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);
    }

    public async Task<List<Question>> GenerateQuestions(string topic, string questionType,
        string language, string difficulty, int questionCount, int optionsCount)
    {
      // Create request based on the API documentation
      var request = new
      {
        questionType,
        sourceContent = new
        {
          type = "topic",
          topic
        },
        language,
        numberOfOptions = optionsCount,
        numberOfQuestions = questionCount,
        difficulty
      };

      // Make API request
      var response = await _httpClient.PostAsJsonAsync($"{_serverUrl}/questions-generator/v2", request);
      response.EnsureSuccessStatusCode();

      var apiResponse = await response.Content.ReadFromJsonAsync<OpExamsResponse>();

      // Convert API response to application question model
      return ConvertToQuestions(apiResponse, questionType);
    }

    public async Task<List<Question>> GenerateQuestionsFromText(string text, string questionType,
        string language, string difficulty, int questionCount, int optionsCount)
    {
      // Create request based on the API documentation
      var request = new
      {
        questionType,
        sourceContent = new
        {
          type = "text",
          text
        },
        language,
        numberOfOptions = optionsCount,
        numberOfQuestions = questionCount,
        difficulty
      };

      // Make API request
      var response = await _httpClient.PostAsJsonAsync($"{_serverUrl}/questions-generator/v2", request);
      response.EnsureSuccessStatusCode();

      var apiResponse = await response.Content.ReadFromJsonAsync<OpExamsResponse>();

      // Convert API response to application question model
      return ConvertToQuestions(apiResponse, questionType);
    }

    public async Task<List<Question>> GenerateQuestionsFromLink(string link, string questionType,
        string language, string difficulty, int questionCount, int optionsCount)
    {
      // Create request based on the API documentation
      var request = new
      {
        questionType,
        sourceContent = new
        {
          type = "link",
          link
        },
        language,
        numberOfOptions = optionsCount,
        numberOfQuestions = questionCount,
        difficulty
      };

      // Make API request
      var response = await _httpClient.PostAsJsonAsync($"{_serverUrl}/questions-generator/v2", request);
      response.EnsureSuccessStatusCode();

      var apiResponse = await response.Content.ReadFromJsonAsync<OpExamsResponse>();

      // Convert API response to application question model
      return ConvertToQuestions(apiResponse, questionType);
    }

    public async Task<List<Question>> GenerateQuestionsFromYoutube(string youtubeLink, string questionType,
        string language, string difficulty, int questionCount, int optionsCount)
    {
      // Create request based on the API documentation
      var request = new
      {
        questionType,
        sourceContent = new
        {
          type = "youtube",
          link = youtubeLink
        },
        language,
        numberOfOptions = optionsCount,
        numberOfQuestions = questionCount,
        difficulty
      };

      // Make API request
      var response = await _httpClient.PostAsJsonAsync($"{_serverUrl}/questions-generator/v2", request);
      response.EnsureSuccessStatusCode();

      var apiResponse = await response.Content.ReadFromJsonAsync<OpExamsResponse>();

      // Convert API response to application question model
      return ConvertToQuestions(apiResponse, questionType);
    }

    public async Task<string> UploadFile(Stream fileStream, string fileName)
    {
      // Create multipart form content
      using var content = new MultipartFormDataContent();
      using var streamContent = new StreamContent(fileStream);

      // Add file content to form
      streamContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileName));
      content.Add(streamContent, "file", fileName);

      // Make API request
      var response = await _httpClient.PostAsync($"{_serverUrl}/questions-generator/v2/upload-file", content);
      response.EnsureSuccessStatusCode();

      // Return the file ID that will be used to reference this file
      var fileId = await response.Content.ReadAsStringAsync();
      return fileId.Trim('"'); // Remove quotation marks from response
    }

    public async Task<List<Question>> GenerateQuestionsFromDocument(string documentId, string questionType,
        string language, string difficulty, int questionCount, int optionsCount)
    {
      // Create request based on the API documentation
      var request = new
      {
        questionType,
        sourceContent = new
        {
          type = "pdf",
          document = documentId
        },
        language,
        numberOfOptions = optionsCount,
        numberOfQuestions = questionCount,
        difficulty
      };

      // Make API request
      var response = await _httpClient.PostAsJsonAsync($"{_serverUrl}/questions-generator/v2", request);
      response.EnsureSuccessStatusCode();

      var apiResponse = await response.Content.ReadFromJsonAsync<OpExamsResponse>();

      // Convert API response to application question model
      return ConvertToQuestions(apiResponse, questionType);
    }

    public async Task<List<Question>> GenerateQuestionsFromImage(string imageId, string questionType,
        string language, string difficulty, int questionCount, int optionsCount)
    {
      // Create request based on the API documentation
      var request = new
      {
        questionType,
        sourceContent = new
        {
          type = "image",
          image = imageId
        },
        language,
        numberOfOptions = optionsCount,
        numberOfQuestions = questionCount,
        difficulty
      };

      // Make API request
      var response = await _httpClient.PostAsJsonAsync($"{_serverUrl}/questions-generator/v2", request);
      response.EnsureSuccessStatusCode();

      var apiResponse = await response.Content.ReadFromJsonAsync<OpExamsResponse>();

      // Convert API response to application question model
      return ConvertToQuestions(apiResponse, questionType);
    }

    public async Task<List<Question>> GenerateQuestionsFromAudio(string audioId, string questionType,
        string language, string difficulty, int questionCount, int optionsCount)
    {
      // Create request based on the API documentation
      var request = new
      {
        questionType,
        sourceContent = new
        {
          type = "audio",
          audio = audioId
        },
        language,
        numberOfOptions = optionsCount,
        numberOfQuestions = questionCount,
        difficulty
      };

      // Make API request
      var response = await _httpClient.PostAsJsonAsync($"{_serverUrl}/questions-generator/v2", request);
      response.EnsureSuccessStatusCode();

      var apiResponse = await response.Content.ReadFromJsonAsync<OpExamsResponse>();

      // Convert API response to application question model
      return ConvertToQuestions(apiResponse, questionType);
    }

    public async Task<List<Question>> GenerateQuestionsFromVideo(string videoId, string questionType,
        string language, string difficulty, int questionCount, int optionsCount)
    {
      // Create request based on the API documentation
      var request = new
      {
        questionType,
        sourceContent = new
        {
          type = "video",
          video = videoId
        },
        language,
        numberOfOptions = optionsCount,
        numberOfQuestions = questionCount,
        difficulty
      };

      // Make API request
      var response = await _httpClient.PostAsJsonAsync($"{_serverUrl}/questions-generator/v2", request);
      response.EnsureSuccessStatusCode();

      var apiResponse = await response.Content.ReadFromJsonAsync<OpExamsResponse>();

      // Convert API response to application question model
      return ConvertToQuestions(apiResponse, questionType);
    }

    public async Task<List<Question>> GenerateQuestionsWithOrderingOrMatching(string topic, string questionType,
        string language, string difficulty, int questionCount, int numberOfRows)
    {
      // Create request based on the API documentation
      var request = new
      {
        questionType,
        sourceContent = new
        {
          type = "topic",
          topic
        },
        language,
        numberOfRows,
        numberOfQuestions = questionCount,
        difficulty
      };

      // Make API request
      var response = await _httpClient.PostAsJsonAsync($"{_serverUrl}/questions-generator/v2", request);
      response.EnsureSuccessStatusCode();

      var apiResponse = await response.Content.ReadFromJsonAsync<OpExamsResponse>();

      // Convert API response to application question model
      return ConvertToQuestions(apiResponse, questionType);
    }

    public async Task<List<Question>> GenerateQuestionsWithMultiSelect(string topic, string questionType,
        string language, string difficulty, int questionCount, int optionsCount, int numberOfCorrectOptions)
    {
      // Create request based on the API documentation
      var request = new
      {
        questionType,
        sourceContent = new
        {
          type = "topic",
          topic
        },
        language,
        numberOfOptions = optionsCount,
        numberOfCorrectOptions,
        numberOfQuestions = questionCount,
        difficulty
      };

      // Make API request
      var response = await _httpClient.PostAsJsonAsync($"{_serverUrl}/questions-generator/v2", request);
      response.EnsureSuccessStatusCode();

      var apiResponse = await response.Content.ReadFromJsonAsync<OpExamsResponse>();

      // Convert API response to application question model
      return ConvertToQuestions(apiResponse, questionType);
    }

    private List<Question> ConvertToQuestions(OpExamsResponse apiResponse, string questionType)
    {
      var questions = new List<Question>();

      if (apiResponse?.Questions == null)
        return questions;

      foreach (var q in apiResponse.Questions)
      {
        var question = new Question
        {
          QuestionText = q.Questions,
          QuestionType = questionType,
          Options = new List<Option>()
        };

        // Handle different question types differently
        switch (questionType.ToUpper())
        {
          case "MCQ":
            // For multiple-choice questions
            if (q.Options != null)
            {
              for (int i = 0; i < q.Options.Count; i++)
              {
                question.Options.Add(new Option
                {
                  OptionText = q.Options[i],
                  IsCorrect = q.Answer == q.Options[i]
                });
              }
            }
            break;

          case "TF":
            // For true/false questions
            question.Options.Add(new Option { OptionText = "True", IsCorrect = q.Answer == "True" });
            question.Options.Add(new Option { OptionText = "False", IsCorrect = q.Answer == "False" });
            break;

          case "OPEN":
          case "SHORTANSWER":
          case "FILLINTHEBLANK":
            // For open-ended questions
            question.CorrectAnswer = q.Answer;
            break;

          case "ORDERING":
            // For ordering questions
            if (q.Options != null)
            {
              // Store the correct order
              var correctOrderItems = q.Answer.Split(',');

              // Add options with their correct positions
              for (int i = 0; i < q.Options.Count; i++)
              {
                int correctPosition = Array.IndexOf(correctOrderItems, q.Options[i]);
                question.Options.Add(new Option
                {
                  OptionText = q.Options[i],
                  OrderPosition = correctPosition
                });
              }
            }
            break;

          case "MATCHING":
            // For matching questions
            if (q.Options != null)
            {
              // Parse the answer format which contains matching pairs
              var matchPairs = q.Answer.Split(';');
              var leftItems = new List<string>();
              var rightItems = new List<string>();

              foreach (var pair in matchPairs)
              {
                var items = pair.Split(',');
                if (items.Length == 2)
                {
                  leftItems.Add(items[0]);
                  rightItems.Add(items[1]);
                }
              }

              // Store left and right columns
              question.LeftColumn = leftItems;
              question.RightColumn = rightItems;
            }
            break;

          case "MULTISELECT":
            // For multi-select questions
            if (q.Options != null)
            {
              // For multi-select, the answer could be multiple comma-separated values
              var correctAnswers = q.Answer.Split(',');

              for (int i = 0; i < q.Options.Count; i++)
              {
                question.Options.Add(new Option
                {
                  OptionText = q.Options[i],
                  IsCorrect = Array.Exists(correctAnswers, answer => answer.Trim() == q.Options[i])
                });
              }
            }
            break;

          default:
            // Default case for any other question types
            if (q.Options != null)
            {
              for (int i = 0; i < q.Options.Count; i++)
              {
                question.Options.Add(new Option
                {
                  OptionText = q.Options[i],
                  IsCorrect = q.Answer == q.Options[i]
                });
              }
            }
            else
            {
              question.CorrectAnswer = q.Answer;
            }
            break;
        }

        questions.Add(question);
      }

      return questions;
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
