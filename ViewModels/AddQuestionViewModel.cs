using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TawtheefTest.ViewModels
{
    public class AddQuestionViewModel
    {
        public long ExamId { get; set; }

        [Required(ErrorMessage = "يرجى اختيار مجموعة الأسئلة")]
        public long QuestionSetId { get; set; }

        [Required(ErrorMessage = "يرجى كتابة نص السؤال")]
        [StringLength(1000, ErrorMessage = "يجب ألا يتجاوز نص السؤال 1000 حرف")]
        public string QuestionText { get; set; }

        [Required(ErrorMessage = "يرجى اختيار نوع السؤال")]
        public string QuestionType { get; set; }

        public bool? TrueFalseAnswer { get; set; }

        public string Answer { get; set; }

        public List<string> Options { get; set; } = new List<string>();

        public long CorrectOptionIndex { get; set; }
    }
}
