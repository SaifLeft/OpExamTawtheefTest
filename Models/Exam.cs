using System;
using System.Collections.Generic;

namespace TawtheefTest.Models
{
  public class Exam
  {
    public int Id { get; set; }
    public int JobId { get; set; }
    public string? Name { get; set; }
    public int QuestionCount { get; set; }
    public string Difficulty { get; set; } // "easy", "medium", "hard"
    public int OptionsCount { get; set; } // For MCQ questions
    public string QuestionType { get; set; } // "MCQ", "TF", etc.
    public int Duration { get; set; } // Duration in minutes
    public bool ShowResultsImmediately { get; set; }
    public DateTime CreatedDate { get; set; }

    // Navigation properties
    public Job? Job { get; set; }
    public ICollection<ExamQuestionSet>? ExamQuestionSets { get; set; }
    public ICollection<CandidateExam>? CandidateExams { get; set; }
  }
}
