using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TawtheefTest.ViewModels
{
    public class CreateExamViewModel
    {
        [Required(ErrorMessage = "الوظيفة مطلوبة")]
        public int JobId { get; set; }

        [Required(ErrorMessage = "اسم الاختبار مطلوب")]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required(ErrorMessage = "مدة الاختبار مطلوبة")]
        [Range(10, 180, ErrorMessage = "مدة الاختبار يجب أن تكون بين 10 و 180 دقيقة")]
        public int Duration { get; set; }

        [Required(ErrorMessage = "عدد الأسئلة مطلوب")]
        [Range(10, 100, ErrorMessage = "عدد الأسئلة يجب أن يكون بين 10 و 100 سؤال")]
        public int QuestionCount { get; set; }

        [Required(ErrorMessage = "مستوى الصعوبة مطلوب")]
        public string Difficulty { get; set; }

        [Required(ErrorMessage = "عدد الخيارات مطلوب")]
        [Range(2, 6, ErrorMessage = "عدد الخيارات يجب أن يكون بين 2 و 6 خيارات")]
        public int OptionsCount { get; set; }

        [Required(ErrorMessage = "نوع الأسئلة مطلوب")]
        public string QuestionType { get; set; }

        [Required(ErrorMessage = "تاريخ بدء الاختبار مطلوب")]
        [DataType(DataType.Date)]
        public DateTime ExamStartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "وقت بدء الاختبار مطلوب")]
        [DataType(DataType.Time)]
        public TimeSpan ExamStartTime { get; set; } = TimeSpan.FromHours(DateTime.Now.Hour);

        [Required(ErrorMessage = "تاريخ انتهاء الاختبار مطلوب")]
        [DataType(DataType.Date)]
        public DateTime ExamEndDate { get; set; } = DateTime.Today.AddDays(3);

        [Required(ErrorMessage = "وقت انتهاء الاختبار مطلوب")]
        [DataType(DataType.Time)]
        public TimeSpan ExamEndTime { get; set; } = TimeSpan.FromHours(DateTime.Now.Hour);

        [Display(Name = "عرض النتائج مباشرة بعد الانتهاء؟")]
        public bool ShowResultsImmediately { get; set; }

        [Display(Name = "إرسال رابط الاختبار للمتقدمين؟")]
        public bool SendExamLinkToApplicants { get; set; }

        // For the view - not submitted
        public IEnumerable<SelectListItem> Jobs { get; set; }
        public IEnumerable<SelectListItem> QuestionTypes { get; set; }
        public IEnumerable<SelectListItem> DifficultiesTypes { get; set; }
    }
}