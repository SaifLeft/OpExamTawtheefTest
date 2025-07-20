using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TawtheefTest.ViewModels
{
    public class EditExamViewModel
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "الوظيفة مطلوبة")]
        public long JobId { get; set; }

        [Required(ErrorMessage = "اسم الاختبار مطلوب")]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required(ErrorMessage = "مدة الاختبار مطلوبة")]
        [Range(10, 180, ErrorMessage = "مدة الاختبار يجب أن تكون بين 10 و 180 دقيقة")]
        public long Duration { get; set; }

        [Required(ErrorMessage = "عدد الأسئلة مطلوب")]
        [Range(10, 100, ErrorMessage = "عدد الأسئلة يجب أن يكون بين 10 و 100 سؤال")]
        public long QuestionCount { get; set; }

        [Required(ErrorMessage = "مستوى الصعوبة مطلوب")]
        public string Difficulty { get; set; }

        [Required(ErrorMessage = "عدد الخيارات مطلوب")]
        [Range(2, 6, ErrorMessage = "عدد الخيارات يجب أن يكون بين 2 و 6 خيارات")]
        public long OptionsCount { get; set; }

        [Required(ErrorMessage = "نوع الأسئلة مطلوب")]
        public string QuestionType { get; set; }

        public DateTime CreatedDate { get; set; }

        // For the view - not submitted
        public IEnumerable<SelectListItem> Jobs { get; set; }
        public IEnumerable<SelectListItem> QuestionTypes { get; set; }
        public IEnumerable<SelectListItem> DifficultiesTypes { get; set; }
    }
}