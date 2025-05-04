using System;
using System.Collections.Generic;

namespace TawtheefTest.ViewModels
{
    public class CandidateExamResultViewModel
    {
        public int Id { get; set; }
        public int CandidateId { get; set; }
        public string CandidateName { get; set; }
        public int ExamId { get; set; }
        public string ExamName { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal? Score { get; set; }
        public string Status { get; set; }
        public bool ShowResultsImmediately { get; set; }
        public List<CandidateAnswerViewModel> Answers { get; set; }
    }

    public class CandidateAnswerViewModel
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public string QuestionType { get; set; }
        public string Answer { get; set; }
        public bool? IsCorrect { get; set; }
        public List<CandidateQuestionOptionViewModel> Options { get; set; }
        public string CorrectAnswer { get; set; }
    }

    public class CandidateQuestionOptionViewModel
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public bool IsCorrect { get; set; }
    }
}
