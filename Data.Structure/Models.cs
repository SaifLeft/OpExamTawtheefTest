using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Enums;

namespace TawtheefTest.Data.Structure
{

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

    [StringLength(8)]
    public int Phone { get; set; }

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
    public string QuestionType { get; set; } // 'MCQ' | 'TF' | 'open' | 'fillInTheBlank' | 'ordering' | 'matching' | 'multiSelect' | 'shortAnswer'

    [Required]
    [StringLength(50)]
    public string Language { get; set; }

    [Required]
    [StringLength(20)]
    public string Difficulty { get; set; }

    [Required]
    [Range(1, 100)]
    public int QuestionCount { get; set; } = 10;

    [Range(2, 10)]
    public int? OptionsCount { get; set; } = 4;

    [Range(2, 10)]
    public int? NumberOfRows { get; set; }

    [Range(1, 10)]
    public string? NumberOfCorrectOptions { get; set; }
    // source Content
    public string ContentSourceType { get; set; } = string.Empty; // Topic/Text/Link/Youtube/Document/Image/Audio/Video
    public string? Content { get; set; } // For Topic/Text
    public string? FileName { get; set; } // For Document/Image/Audio/Video
    public string? FileUploadedCode { get; set; } // For Document/Image/Audio/Video
    [StringLength(1000)]
    public string? Url { get; set; } // For Link/Youtube
    [StringLength(10000)]

    [Required]
    public QuestionSetStatus Status { get; set; } = QuestionSetStatus.Pending;
    public int? RetryCount { get; set; } = 0;
    [StringLength(1000)]
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
    public virtual ICollection<ExamQuestionSet> ExamQuestionSets { get; set; } = new List<ExamQuestionSet>();
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

    public int Duration { get; set; } = 30; // Duration in minutes

    // النسبة المئوية للنجاح
    public decimal? PassPercentage { get; set; } = 60;
    public int TotalQuestionsPerCandidate { get; set; } = 30;

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public bool ShowResultsImmediately { get; set; } = false;
    public bool SendExamLinkToApplicants { get; set; } = false;

    // Navigation properties
    public virtual ICollection<ExamQuestionSet> ExamQuestionSets { get; set; } = new List<ExamQuestionSet>();
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
    [Required]
    public int QuestionSetId { get; set; }

    // Order of the question set in the exam
    public int DisplayOrder { get; set; }

    [ForeignKey("ExamId")]
    public virtual Exam Exam { get; set; } = null!;

    [ForeignKey("QuestionSetId")]
    public virtual QuestionSet QuestionSet { get; set; } = null!;
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
    public int Index { get; set; }

    [Required]
    [StringLength(100)]
    public string QuestionType { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string QuestionText { get; set; } = string.Empty;

    // مستوى صعوبة السؤال والنقاط المرتبطة به
    [StringLength(20)]
    public string DifficultyLevel { get; set; } // easy, medium, hard

    public int Points { get; set; } = 1; // النقاط المخصصة للسؤال حسب صعوبته

    // ترتيب عرض السؤال في الاختبار
    public int DisplayOrder { get; set; }

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

    public int? CorrectOrder { get; set; }
    public int? DisplayOrder { get; set; }
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
    public string Status { get; set; }

    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }

    public decimal? Score { get; set; }

    // نظام التقييم المحسن
    public int TotalPoints { get; set; } = 0; // إجمالي النقاط المحققة
    public int MaxPossiblePoints { get; set; } = 0; // أقصى نقاط ممكنة
    public int EasyQuestionsCorrect { get; set; } = 0; // عدد الأسئلة السهلة الصحيحة
    public int MediumQuestionsCorrect { get; set; } = 0; // عدد الأسئلة المتوسطة الصحيحة
    public int HardQuestionsCorrect { get; set; } = 0; // عدد الأسئلة الصعبة الصحيحة
    public TimeSpan? CompletionDuration { get; set; } // مدة إكمال الاختبار
    public int RankPosition { get; set; } = 0; // ترتيب المرشح

    // إجمالي عدد الأسئلة في الاختبار
    public int TotalQuestions { get; set; }

    // عدد الأسئلة التي تم الإجابة عليها
    public int CompletedQuestions { get; set; }

    // هل تم استبدال سؤال خلال هذا الاختبار
    public bool QuestionReplaced { get; set; } = false;

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

    public bool? TrueFalseAnswer { get; set; }

    public int? SelectedOptionId { get; set; }

    [StringLength(2000)]
    public string? SelectedOptionsJson { get; set; } // For multi-select questions

    [StringLength(2000)]
    public string? MatchingPairsJson { get; set; } // For matching questions

    [StringLength(2000)]
    public string? OrderingJson { get; set; } // For ordering questions

    public bool? IsCorrect { get; set; }

    // تعليم السؤال للمراجعة لاحقاً
    public bool IsFlagged { get; set; } = false;

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
