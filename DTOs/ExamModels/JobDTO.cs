using System;

namespace TawtheefTest.DTOs.ExamModels
{
    public class JobDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CandidateCount { get; set; }
        public int ExamCount { get; set; }
    }
}