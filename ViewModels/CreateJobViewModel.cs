using System.ComponentModel.DataAnnotations;

namespace TawtheefTest.ViewModels
{
  public class CreateJobViewModel
  {
    [Required(ErrorMessage = "عنوان الوظيفة مطلوب")]
    [Display(Name = "عنوان الوظيفة")]
    [StringLength(100, ErrorMessage = "يجب ألا يتجاوز العنوان 100 حرف")]
    public string Name { get; set; }
  }
}
