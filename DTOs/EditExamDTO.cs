using System.ComponentModel.DataAnnotations;

namespace TawtheefTest.DTOs
{
  public class EditExamDTO
  {
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم الاختبار مطلوب")]
    [Display(Name = "اسم الاختبار")]
    public string Name { get; set; }

    [Required(ErrorMessage = "الوظيفة مطلوبة")]
    [Display(Name = "الوظيفة")]
    public int JobId { get; set; }

    [Display(Name = "وصف الاختبار")]
    public string Description { get; set; }

    [Required(ErrorMessage = "مدة الاختبار مطلوبة")]
    [Range(1, 240, ErrorMessage = "مدة الاختبار يجب أن تكون بين 1 و 240 دقيقة")]
    [Display(Name = "مدة الاختبار (دقيقة)")]
    public int Duration { get; set; }

    [Required(ErrorMessage = "تاريخ بداية الاختبار مطلوب")]
    [Display(Name = "تاريخ بداية الاختبار")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "تاريخ نهاية الاختبار مطلوب")]
    [Display(Name = "تاريخ نهاية الاختبار")]
    public DateTime EndDate { get; set; }

    [Display(Name = "إظهار النتائج للمتقدم مباشرة")]
    public bool ShowResultsImmediately { get; set; }

    [Display(Name = "إرسال روابط الاختبار للمتقدمين")]
    public bool SendExamLinkToApplicants { get; set; }
  }
}
