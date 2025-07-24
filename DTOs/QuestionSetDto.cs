using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TawtheefTest.Data.Structure;
using TawtheefTest.Enums;
using TawtheefTest.ViewModels;

namespace TawtheefTest.DTOs
{
  public class QuestionSetDto
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string QuestionType { get; set; }
    public QuestionSetLanguage Language { get; set; }
    public string Difficulty { get; set; }
    public int QuestionCount { get; set; }
    public int? OptionsCount { get; set; }
    public QuestionSetStatus Status { get; set; }
    public string StatusDescription { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool QuestionsGenerated { get; set; }
    public int UsageCount { get; set; }
    public List<string> UsedInExams { get; set; }
    [JsonIgnore]
    public string StatusClass => GetStatusClass();
    [JsonIgnore]
    public string StatusIcon => GetStatusIcon();

    private string GetStatusClass()
    {
      return Status switch
      {
        QuestionSetStatus.Pending => "bg-warning",
        QuestionSetStatus.Processing => "bg-info",
        QuestionSetStatus.Completed => "bg-success",
        QuestionSetStatus.Failed => "bg-danger",
        _ => "bg-secondary"
      };
    }

    private string GetStatusIcon()
    {
      return Status switch
      {
        QuestionSetStatus.Pending => "hourglass",
        QuestionSetStatus.Processing => "arrow-repeat",
        QuestionSetStatus.Completed => "check-circle",
        QuestionSetStatus.Failed => "exclamation-triangle",
        _ => "question-circle"
      };
    }

    // خصائص مصدر المحتوى مباشرة
    public string ContentSourceType { get; set; }
    public string Content { get; set; }
    public string Url { get; set; }
    public string FileName { get; set; }
    public string FileUploadedCode { get; set; }

    public IEnumerable<QuestionDto> Questions { get; set; }
  }

  
}
