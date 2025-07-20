using System;
using System.Collections.Generic;

namespace TawtheefTest.DTOs.ExamModels
{
    public class ExamDTO
    {
        public long Id { get; set; }
        public long JobId { get; set; }
        public string JobName { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public long QuestionCount { get; set; }
        public string Difficulty { get; set; }
        public long OptionsCount { get; set; }
        public string QuestionType { get; set; }
        public long Duration { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}