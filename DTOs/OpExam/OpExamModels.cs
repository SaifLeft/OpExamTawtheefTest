using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TawtheefTest.DTOs.OpExam
{
  #region Request Models

  public class OpExamQuestionGenerationRequest
  {
    [JsonPropertyName("questionType")]
    public string QuestionType { get; set; }

    [JsonPropertyName("sourceContent")]
    public SourceContent SourceContent { get; set; }

    [JsonPropertyName("language")]
    public string Language { get; set; } = "Arabic";

    [JsonPropertyName("numberOfOptions")]
    public int NumberOfOptions { get; set; } = 4;

    [JsonPropertyName("numberOfQuestions")]
    public int NumberOfQuestions { get; set; } = 10;

    [JsonPropertyName("difficulty")]
    public string Difficulty { get; set; } = "auto";

    [JsonPropertyName("numberOfRows")]
    public int? NumberOfRows { get; set; }

    [JsonPropertyName("numberOfCorrectOptions")]
    public string? NumberOfCorrectOptions { get; set; } = "auto";
  }

  public class SourceContent
  {
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("topic")]
    public string Topic { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("link")]
    public string Link { get; set; }

    [JsonPropertyName("document")]
    public string Document { get; set; }

    [JsonPropertyName("image")]
    public string Image { get; set; }

    [JsonPropertyName("audio")]
    public string Audio { get; set; }

    [JsonPropertyName("video")]
    public string Video { get; set; }
  }

  public class FileUploadResponse
  {
    [JsonPropertyName("url")]
    public string Url { get; set; }
  }

  #endregion

  #region Response Models

  public class OpExamQuestionGenerationResponse
  {
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("questions")]
    public List<OpExamQuestion> Questions { get; set; }
  }

  public class OpExamQuestion
  {
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("question")]
    public string QuestionText { get; set; }

    [JsonPropertyName("options")]
    public List<string> Options { get; set; }

    [JsonPropertyName("answerIndex")]
    public int? AnswerIndex { get; set; }

    [JsonPropertyName("answer")]
    public string Answer { get; set; }

    [JsonPropertyName("sampleAnswer")]
    public string SampleAnswer { get; set; }

    [JsonPropertyName("correctlyOrdered")]
    public List<string> CorrectlyOrdered { get; set; }

    [JsonPropertyName("shuffledOrder")]
    public List<string> ShuffledOrder { get; set; }

    [JsonPropertyName("instructionText")]
    public string InstructionText { get; set; }
  }

  public class OpExamTFQuestionResponse
  {
    public string title { get; set; }
    [JsonPropertyName("questions")]
    public List<TFQuestion> Questions { get; set; }
  }

  public class TFQuestion
  {
    public int Index { get; set; }
    [JsonPropertyName("question")]
    public string QuestionText { get; set; }
    [JsonPropertyName("answer")]
    public bool Answer { get; set; }
    public string id { get; set; }
  }

  #endregion
}
