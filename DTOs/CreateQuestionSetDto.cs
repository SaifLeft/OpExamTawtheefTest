using System.ComponentModel.DataAnnotations;

namespace TawtheefTest.DTOs
{
    public class CreateQuestionSetDto
    {
        [Required(ErrorMessage = "اسم المجموعة مطلوب")]
        [Display(Name = "اسم المجموعة")]
        public string Name { get; set; }

        [Required(ErrorMessage = "الاختبار مطلوب")]
        [Display(Name = "الاختبار")]
        public int ExamId { get; set; }

        [Display(Name = "وصف المجموعة")]
        public string Description { get; set; }

        [Required(ErrorMessage = "نوع الأسئلة مطلوب")]
        [Display(Name = "نوع الأسئلة")]
        public string QuestionType { get; set; } // MCQ, TF, open, fillInTheBlank, ordering, matching, multiSelect, shortAnswer

        [Required(ErrorMessage = "اللغة مطلوبة")]
        [Display(Name = "اللغة")]
        public string Language { get; set; }

        [Required(ErrorMessage = "عدد الأسئلة مطلوب")]
        [Range(1, 50, ErrorMessage = "عدد الأسئلة يجب أن يكون بين 1 و 50")]
        [Display(Name = "عدد الأسئلة")]
        public int QuestionCount { get; set; }

        [Required(ErrorMessage = "عدد الخيارات مطلوب")]
        [Range(2, 10, ErrorMessage = "عدد الخيارات يجب أن يكون بين 2 و 10")]
        [Display(Name = "عدد الخيارات")]
        public int OptionsCount { get; set; }

        [Required(ErrorMessage = "مستوى الصعوبة مطلوب")]
        [Display(Name = "مستوى الصعوبة")]
        public string Difficulty { get; set; }

        [Display(Name = "نوع المحتوى")]
        public string ContentType { get; set; } // topic, text, link, youtube, document, image, audio, video

        [Display(Name = "الموضوع")]
        public string Topic { get; set; }

        [Display(Name = "نص المحتوى")]
        public string TextContent { get; set; }

        [Display(Name = "رابط")]
        [Url(ErrorMessage = "الرجاء إدخال رابط صحيح")]
        public string LinkUrl { get; set; }

        [Display(Name = "رابط يوتيوب")]
        [Url(ErrorMessage = "الرجاء إدخال رابط يوتيوب صحيح")]
        public string YoutubeUrl { get; set; }

        [Display(Name = "مرجع الملف")]
        public string FileReference { get; set; }

        [Display(Name = "عدد العناصر")]
        [Range(2, 10, ErrorMessage = "عدد العناصر يجب أن يكون بين 2 و 10")]
        public int? NumberOfRows { get; set; }

        [Display(Name = "عدد الإجابات الصحيحة")]
        [Range(1, 5, ErrorMessage = "عدد الإجابات الصحيحة يجب أن يكون بين 1 و 5")]
        public int? NumberOfCorrectOptions { get; set; }
    }
}