using System.ComponentModel.DataAnnotations;
using TawtheefTest.Data.Structure;
using TawtheefTest.Enum;

namespace TawtheefTest.ViewModels
{
  public class CreateQuestionSetViewModel
  {
    [Required]
    [Display(Name = "اسم المجموعة")]
    [StringLength(100)]
    public string Name { get; set; }

    [Display(Name = "وصف المجموعة")]
    [StringLength(500)]
    public string Description { get; set; }

    [Required]
    [Display(Name = "نوع الأسئلة")]
    public QuestionTypeEnum QuestionType { get; set; }

    [Required]
    [Display(Name = "مستوى الصعوبة")]
    [StringLength(20)]
    public string Difficulty { get; set; } = "easy";

    [Required]
    [Display(Name = "عدد الأسئلة")]
    [Range(1, 100)]
    public int QuestionCount { get; set; } = 10;

    [Display(Name = "عدد الخيارات")]
    [Range(2, 10)]
    public int? OptionsCount { get; set; } = 4;

    [Display(Name = "الاختبار")]
    public int ExamId { get; set; }

    // Additional properties needed for views
    [Required]
    [Display(Name = "اللغة")]
    [StringLength(50)]
    public string Language { get; set; } = "English";

    [Display(Name = "عدد العناصر في التطابق/الترتيب")]
    [Range(2, 10)]
    public int? NumberOfRows { get; set; }

    [Display(Name = "عدد الإجابات الصحيحة")]
    [Range(1, 10)]
    public int? NumberOfCorrectOptions { get; set; }

    [Display(Name = "الموضوع")]
    [StringLength(1000)]
    public string Topic { get; set; }

    [Display(Name = "النص")]
    [StringLength(10000)]
    public string TextContent { get; set; }

    [Display(Name = "الرابط")]
    [StringLength(1000)]
    public string LinkUrl { get; set; }

    [Display(Name = "رابط يوتيوب")]
    [StringLength(1000)]
    public string YoutubeUrl { get; set; }

    [Display(Name = "مرجع الملف")]
    [StringLength(1000)]
    public string FileReference { get; set; }
  }
}
