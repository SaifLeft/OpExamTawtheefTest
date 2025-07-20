using System.ComponentModel.DataAnnotations;

namespace TawtheefTest.ViewModels
{
  public class CreateQuestionSetViewModel
  {
    [Required(ErrorMessage = "اسم المجموعة مطلوب")]
    [Display(Name = "اسم المجموعة")]
    public string Name { get; set; }

    [Display(Name = "وصف المجموعة")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "نوع الأسئلة مطلوب")]
    [Display(Name = "نوع الأسئلة")]
    public string QuestionType { get; set; }

    [Required(ErrorMessage = "اللغة مطلوبة")]
    [Display(Name = "اللغة")]
    public string Language { get; set; } = "Arabic";

    [Required(ErrorMessage = "مستوى الصعوبة مطلوب")]
    [Display(Name = "مستوى الصعوبة")]
    public string Difficulty { get; set; } = "auto";

    [Required(ErrorMessage = "عدد الأسئلة مطلوب")]
    [Range(1, 100, ErrorMessage = "يجب أن يكون عدد الأسئلة بين 1 و 100")]
    [Display(Name = "عدد الأسئلة")]
    public long QuestionCount { get; set; } = 10;

    [Range(2, 10, ErrorMessage = "يجب أن يكون عدد الخيارات بين 2 و 10")]
    [Display(Name = "عدد الخيارات")]
    public long? OptionsCount { get; set; }

    [Range(1, 5, ErrorMessage = "يجب أن يكون عدد الإجابات الصحيحة بين 1 و 5")]
    [Display(Name = "عدد الإجابات الصحيحة")]
    public long? NumberOfCorrectOptions { get; set; }

    [Range(2, 10, ErrorMessage = "يجب أن يكون عدد العناصر بين 2 و 10")]
    [Display(Name = "عدد العناصر في التطابق/الترتيب")]
    public long? NumberOfRows { get; set; }

    [Required(ErrorMessage = "نوع مصدر المحتوى مطلوب")]
    [Display(Name = "نوع مصدر المحتوى")]
    public string ContentSourceType { get; set; }

    [Display(Name = "الموضوع")]
    public string? Topic { get; set; }

    [Display(Name = "النص")]
    public string? TextContent { get; set; }

    [Display(Name = "الرابط")]
    [Url(ErrorMessage = "الرجاء إدخال رابط صحيح")]
    public string? LinkUrl { get; set; }

    [Display(Name = "رابط يوتيوب")]
    [Url(ErrorMessage = "الرجاء إدخال رابط يوتيوب صحيح")]
    public string? YoutubeUrl { get; set; }

    [Display(Name = "الملف")]
    public IFormFile? File { get; set; }
  }
}
