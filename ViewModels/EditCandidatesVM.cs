using System.ComponentModel.DataAnnotations;

namespace TawtheefTest.ViewModels
{
  public class EditCandidatesVM
  {
    [Required(ErrorMessage = "الرقم التعريفي مطلوب")]
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم المرشح مطلوب")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "الاسم يجب أن يكون أطول من 3 أحرف")]
    [Display(Name = "اسم المرشح")]
    public string Name { get; set; }

    [Required(ErrorMessage = "رقم الهاتف مطلوب")]
    [Phone(ErrorMessage = "الرجاء ادخال رقم الهاتف بشكل صحيح")]
    [StringLength(8, MinimumLength = 8, ErrorMessage = "الرجاء ادخال رقم الهاتف بشكل صحيح")]
    [Display(Name = "رقم الهاتف")]
    public string PhoneNumber { get; set; }

    [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صالح")]
    [Display(Name = "البريد الإلكتروني")]
    public string Email { get; set; }

    [Required(ErrorMessage = "الوظيفة مطلوبة")]
    [Display(Name = "الوظيفة")]
    public int JobId { get; set; }
  }
}
