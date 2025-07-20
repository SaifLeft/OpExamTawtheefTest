using System;

namespace TawtheefTest.DTOs.ExamModels
{
    public class JobDTO
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public DateTime CreatedDate { get; set; }
        public long CandidateCount { get; set; }
        public long ExamCount { get; set; }
    }
}