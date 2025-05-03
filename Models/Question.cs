using System;
using System.Collections.Generic;

namespace TawtheefTest.Models
{
  public class Question
  {
    public int Id { get; set; }
    public int ExamId { get; set; }
    public string QuestionText { get; set; }
    public string QuestionType { get; set; }
    public string CorrectAnswer { get; set; } // For open-ended questions

    // Navigation properties
    // For matching questions
    public List<string> LeftColumn { get; set; }
    public List<string> RightColumn { get; set; }
    public Exam Exam { get; set; }
    public ICollection<Option> Options { get; set; } // For questions with options
    public ICollection<CandidateAnswer> CandidateAnswers { get; set; }
  }
}
