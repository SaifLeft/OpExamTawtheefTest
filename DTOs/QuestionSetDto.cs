using System;
using System.ComponentModel.DataAnnotations;

namespace TawtheefTest.DTOs
{
    public class QuestionSetDto
    {
        public int Id { get; set; }

        [Display(Name = "اسم المجموعة")]
        public string Name { get; set; }

        [Display(Name = "الاختبار")]
        public int ExamId { get; set; }

        [Display(Name = "وصف المجموعة")]
        public string Description { get; set; }

        [Display(Name = "نوع الأسئلة")]
        public string QuestionType { get; set; }

        [Display(Name = "اللغة")]
        public string Language { get; set; }

        [Display(Name = "عدد الأسئلة")]
        public int QuestionCount { get; set; }

        [Display(Name = "عدد الخيارات")]
        public int OptionsCount { get; set; }

        [Display(Name = "مستوى الصعوبة")]
        public string Difficulty { get; set; }

        [Display(Name = "نوع المحتوى")]
        public string ContentType { get; set; }

        [Display(Name = "الموضوع")]
        public string Topic { get; set; }

        [Display(Name = "نص المحتوى")]
        public string TextContent { get; set; }

        [Display(Name = "رابط")]
        public string LinkUrl { get; set; }

        [Display(Name = "رابط يوتيوب")]
        public string YoutubeUrl { get; set; }

        [Display(Name = "مرجع الملف")]
        public string FileReference { get; set; }

        [Display(Name = "عدد العناصر")]
        public int? NumberOfRows { get; set; }

        [Display(Name = "عدد الإجابات الصحيحة")]
        public int? NumberOfCorrectOptions { get; set; }

        [Display(Name = "الحالة")]
        public string Status { get; set; }

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "تاريخ الاكتمال")]
        public DateTime? CompletedDate { get; set; }
    }

    
}
