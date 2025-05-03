using System;
using System.Collections.Generic;

namespace TawtheefTest.Models
{
    public class CandidateExam
    {
        public int Id { get; set; }
        public int CandidateId { get; set; }
        public int ExamId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public double? Score { get; set; }
        public bool IsCompleted { get; set; }

        // Navigation properties
        public Candidate Candidate { get; set; }
        public Exam Exam { get; set; }
        public ICollection<CandidateAnswer> CandidateAnswers { get; set; }
    }
}