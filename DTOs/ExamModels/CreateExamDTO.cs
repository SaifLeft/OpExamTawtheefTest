using System;

namespace TawtheefTest.DTOs.ExamModels
{
    public class CreateExamDTO
    {
        public int JobId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Duration { get; set; }
        public int QuestionCount { get; set; }
        public string Difficulty { get; set; }
        public int OptionsCount { get; set; }
        public string QuestionType { get; set; }
    }
}