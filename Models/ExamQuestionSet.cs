using System;
using System.Collections.Generic;

namespace TawtheefTest.Models
{
    public class ExamQuestionSet
    {
        public int Id { get; set; }
        public int ExamId { get; set; }
        public int QuestionSetId { get; set; }
        public int DisplayOrder { get; set; }

        // Navigation properties
        public Exam Exam { get; set; }
        public QuestionSet QuestionSet { get; set; }
    }
}
