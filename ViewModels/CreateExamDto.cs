using System.ComponentModel.DataAnnotations;
using TawtheefTest.Data.Structure;

namespace TawtheefTest.ViewModels
{
  public class CreateExamDto
  {
    [Required]
    [Display(Name = "اسم الاختبار")]
    [StringLength(100)]
    public string Name { get; set; }

    [Display(Name = "وصف الاختبار")]
    [StringLength(500)]
    public string Description { get; set; }

    [Required]
    [Display(Name = "الوظيفة")]
    public int JobId { get; set; }

    [Display(Name = "مدة الاختبار (بالدقائق)")]
    [Range(1, 180)]
    public long? Duration { get; set; }

    [Display(Name = "تاريخ البدء")]
    public System.DateTime? StartDate { get; set; }

    [Display(Name = "تاريخ الانتهاء")]
    public System.DateTime? EndDate { get; set; }
  }
}
