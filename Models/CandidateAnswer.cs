using System;

namespace TawtheefTest.Models
{
    public class CandidateAnswer
    {
        public int Id { get; set; }
        public int CandidateExamId { get; set; }
        public int QuestionId { get; set; }
        public string Answer { get; set; }
        public bool IsCorrect { get; set; }

        // Navigation properties
        public CandidateExam CandidateExam { get; set; }
        public Question Question { get; set; }
    }
}