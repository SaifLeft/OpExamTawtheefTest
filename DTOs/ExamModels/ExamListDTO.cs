using System;
using System.Collections.Generic;

namespace TawtheefTest.DTOs.ExamModels
{
    public class ExamListDTO
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public string JobName { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int QuestionCount { get; set; }
        public string Difficulty { get; set; }
        public int OptionsCount { get; set; }
        public string QuestionType { get; set; }
        public int Duration { get; set; }
        public DateTime CreatedDate { get; set; }
        public int QuestionsCount { get; set; }
    }
}