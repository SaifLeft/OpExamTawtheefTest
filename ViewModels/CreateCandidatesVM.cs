using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TawtheefTest.ViewModels
{
  public class CreateCandidatesVM
  {
    [Required(ErrorMessage = "اسم المرشح مطلوب")]
    [Display(Name = "اسم المرشح")]
    public string Name { get; set; }

    [Required(ErrorMessage = "رقم الهاتف مطلوب")]
    [Display(Name = "رقم الهاتف")]
    public int Phone { get; set; }

    [Required(ErrorMessage = "الوظيفة مطلوبة")]
    [Display(Name = "الوظيفة")]
    public int JobId { get; set; }
  }
}
