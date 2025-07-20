using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TawtheefTest.ViewModels
{
    public class CreateExamViewModel
    {
        [Required(ErrorMessage = "الوظيفة مطلوبة")]
        public long JobId { get; set; }

        [Required(ErrorMessage = "اسم الاختبار مطلوب")]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required(ErrorMessage = "مدة الاختبار مطلوبة")]
        [Range(10, 180, ErrorMessage = "مدة الاختبار يجب أن تكون بين 10 و 180 دقيقة")]
        public long Duration { get; set; }

        [Required(ErrorMessage = "تاريخ بدء الاختبار مطلوب")]
        [DataType(DataType.Date)]
        public DateTime ExamStartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "تاريخ انتهاء الاختبار مطلوب")]
        [DataType(DataType.Date)]
        public DateTime ExamEndDate { get; set; } = DateTime.Today.AddDays(3);

        [Display(Name = "عرض النتائج مباشرة بعد الانتهاء؟")]
        public long ShowResultsImmediately { get; set; }

        [Display(Name = "إرسال رابط الاختبار للمتقدمين؟")]
        public long SendExamLinkToApplicants { get; set; }
    }
}
