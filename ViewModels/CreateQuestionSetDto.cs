using System.ComponentModel.DataAnnotations;
using TawtheefTest.Data.Structure;

namespace TawtheefTest.ViewModels
{
  public class CreateQuestionSetDto
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
  }
}
