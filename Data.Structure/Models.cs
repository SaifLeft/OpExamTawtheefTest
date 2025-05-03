using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TawtheefTest.Data.Structure
{
  // Enums
  public enum QuestionSetStatus
  {
    Pending,
    Processing,
    Completed,
    Failed
  }

  public enum ContentSourceType
  {
    Topic,
    Text,
    Link,
    Youtube,
    Document,
    Image,
    Audio,
    Video
  }

  public enum ExamStatus
  {
    Draft,
    Published,
    Archived
  }

  public enum CandidateExamStatus
  {
    NotStarted,
    InProgress,
    Completed,
    Abandoned
  }

  public enum QuestionTypeEnum
  {
    MCQ,
    TF,
    Open,
    FillInTheBlank,
    Ordering,
    Matching,
    MultiSelect,
    ShortAnswer
  }

  public enum FileType
  {
    Document,
    Image,
    Audio,
    Video,
    Other
  }

  // Models
  public class Job
  {
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<Candidate> Candidates { get; set; } = new List<Candidate>();
    public virtual ICollection<Exam> Exams { get; set; } = new List<Exam>();
  }

  public class Candidate
  {
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(20)]
    public int PhoneNumber { get; set; }

    [Required]
    public int JobId { get; set; }

    [ForeignKey("JobId")]
    public virtual Job Job { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<CandidateExam> CandidateExams { get; set; } = new List<CandidateExam>();
  }

  public class ContentSource
  {
    [Key]
    public int Id { get; set; }

    [Required]
    public ContentSourceType Type { get; set; }

    [StringLength(10000)]
    public string? Content { get; set; }

    [StringLength(1000)]
    public string? Url { get; set; }

    public int? UploadedFileId { get; set; }

    [ForeignKey("UploadedFileId")]
    public virtual UploadedFile? UploadedFile { get; set; }

    [Required]
    public int QuestionSetId { get; set; }

    [ForeignKey("QuestionSetId")]
    public virtual QuestionSet QuestionSet { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  }

  public class UploadedFile
  {
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(1000)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string FileId { get; set; } = string.Empty;

    [Required]
    public FileType FileType { get; set; }

    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    [StringLength(1000)]
    public string? FilePath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<ContentSource> ContentSources { get; set; } = new List<ContentSource>();
  }

  public class QuestionSet
  {
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    // Question generation settings
    [Required]
    public QuestionTypeEnum QuestionType { get; set; } = QuestionTypeEnum.MCQ;

    [Required]
    [StringLength(50)]
    public string Language { get; set; } = "English";

    [Required]
    [StringLength(20)]
    public string Difficulty { get; set; } = "easy";

    [Required]
    [Range(1, 100)]
    public int QuestionCount { get; set; } = 10;

    [Range(2, 10)]
    public int? OptionsCount { get; set; } = 4;

    [Range(2, 10)]
    public int? NumberOfRows { get; set; }

    [Range(1, 10)]
    public int? NumberOfCorrectOptions { get; set; }

    // Processing status
    [Required]
    public QuestionSetStatus Status { get; set; } = QuestionSetStatus.Pending;

    // Error details if processing failed
    [StringLength(1000)]
    public string? ErrorMessage { get; set; }

    // Navigation properties
    public virtual ICollection<ContentSource> ContentSources { get; set; } = new List<ContentSource>();
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
    public virtual ICollection<ExamQuestionSet> ExamQuestionSets { get; set; } = new List<ExamQuestionSet>();

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
  }

  public class Exam
  {
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public int JobId { get; set; }

    [ForeignKey("JobId")]
    public virtual Job Job { get; set; } = null!;

    [Required]
    public ExamStatus Status { get; set; } = ExamStatus.Draft;

    public int? Duration { get; set; } // Duration in minutes

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // Navigation properties
    public virtual ICollection<ExamQuestionSet> ExamQuestionSets { get; set; } = new List<ExamQuestionSet>();
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
    public virtual ICollection<CandidateExam> CandidateExams { get; set; } = new List<CandidateExam>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
  }

  public class ExamQuestionSet
  {
    [Key]
    public int Id { get; set; }

    [Required]
    public int ExamId { get; set; }

    [ForeignKey("ExamId")]
    public virtual Exam Exam { get; set; } = null!;

    [Required]
    public int QuestionSetId { get; set; }

    [ForeignKey("QuestionSetId")]
    public virtual QuestionSet QuestionSet { get; set; } = null!;

    // Order of the question set in the exam
    public int DisplayOrder { get; set; }
  }

  public class Question
  {
    [Key]
    public int Id { get; set; }

    [Required]
    public int QuestionSetId { get; set; }

    [ForeignKey("QuestionSetId")]
    public virtual QuestionSet QuestionSet { get; set; } = null!;

    [Required]
    public int ExamId { get; set; }

    [ForeignKey("ExamId")]
    public virtual Exam Exam { get; set; } = null!;

    [Required]
    public int Index { get; set; }

    [Required]
    [StringLength(1000)]
    public string QuestionText { get; set; } = string.Empty;

    [Required]
    public QuestionTypeEnum QuestionType { get; set; }

    // For multiple choice questions
    public virtual ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
    public virtual ICollection<OptionChoice> OptionChoices { get; set; } = new List<OptionChoice>();

    // For matching questions
    public virtual ICollection<MatchingPair> MatchingPairs { get; set; } = new List<MatchingPair>();

    // For ordering questions
    public virtual ICollection<OrderingItem> OrderingItems { get; set; } = new List<OrderingItem>();

    // Answer fields
    public int? AnswerIndex { get; set; }

    [StringLength(1000)]
    public string? Answer { get; set; }

    // For true/false questions
    public bool? TrueFalseAnswer { get; set; }

    // For advanced question types
    [StringLength(1000)]
    public string? InstructionText { get; set; }

    [StringLength(2000)]
    public string? SampleAnswer { get; set; }

    // External ID from the API
    [StringLength(100)]
    public string? ExternalId { get; set; }

    // Navigation properties
    public virtual ICollection<CandidateAnswer> CandidateAnswers { get; set; } = new List<CandidateAnswer>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  }

  public class QuestionOption
  {
    [Key]
    public int Id { get; set; }

    [Required]
    public int QuestionId { get; set; }

    [ForeignKey("QuestionId")]
    public virtual Question Question { get; set; } = null!;

    [Required]
    public int Index { get; set; }

    [Required]
    [StringLength(1000)]
    public string Text { get; set; } = string.Empty;

    // For multi-select questions (whether this option is correct)
    public bool IsCorrect { get; set; } = false;
  }

  public class OptionChoice
  {
    [Key]
    public int Id { get; set; }

    [Required]
    public int QuestionId { get; set; }

    [ForeignKey("QuestionId")]
    public virtual Question Question { get; set; } = null!;

    [Required]
    [StringLength(1000)]
    public string Text { get; set; } = string.Empty;

    [Required]
    public bool IsCorrect { get; set; }

    public int DisplayOrder { get; set; }
  }

  public class MatchingPair
  {
    [Key]
    public int Id { get; set; }

    [Required]
    public int QuestionId { get; set; }

    [ForeignKey("QuestionId")]
    public virtual Question Question { get; set; } = null!;

    [Required]
    [StringLength(1000)]
    public string LeftItem { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string RightItem { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }
  }

  public class OrderingItem
  {
    [Key]
    public int Id { get; set; }

    [Required]
    public int QuestionId { get; set; }

    [ForeignKey("QuestionId")]
    public virtual Question Question { get; set; } = null!;

    [Required]
    [StringLength(1000)]
    public string Text { get; set; } = string.Empty;

    [Required]
    public int CorrectOrder { get; set; }

    public int DisplayOrder { get; set; }
  }

  public class CandidateExam
  {
    [Key]
    public int Id { get; set; }

    [Required]
    public int CandidateId { get; set; }

    [ForeignKey("CandidateId")]
    public virtual Candidate Candidate { get; set; } = null!;

    [Required]
    public int ExamId { get; set; }

    [ForeignKey("ExamId")]
    public virtual Exam Exam { get; set; } = null!;

    [Required]
    public CandidateExamStatus Status { get; set; } = CandidateExamStatus.NotStarted;

    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }

    public decimal? Score { get; set; }

    // Navigation properties
    public virtual ICollection<CandidateAnswer> CandidateAnswers { get; set; } = new List<CandidateAnswer>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
  }

  public class CandidateAnswer
  {
    [Key]
    public int Id { get; set; }

    [Required]
    public int CandidateExamId { get; set; }

    [ForeignKey("CandidateExamId")]
    public virtual CandidateExam CandidateExam { get; set; } = null!;

    [Required]
    public int QuestionId { get; set; }

    [ForeignKey("QuestionId")]
    public virtual Question Question { get; set; } = null!;

    [StringLength(2000)]
    public string? AnswerText { get; set; }

    public int? SelectedOptionId { get; set; }

    [StringLength(2000)]
    public string? SelectedOptionsJson { get; set; } // For multi-select questions

    [StringLength(2000)]
    public string? MatchingPairsJson { get; set; } // For matching questions

    [StringLength(2000)]
    public string? OrderingJson { get; set; } // For ordering questions

    public bool? IsCorrect { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
  }

  public class OTPVerification
  {
    [Key]
    public int Id { get; set; }

    [Required]
    public int PhoneNumber { get; set; }

    [Required]
    [StringLength(6)]
    public string OTPCode { get; set; } = string.Empty;

    public bool IsVerified { get; set; } = false;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  }
}
