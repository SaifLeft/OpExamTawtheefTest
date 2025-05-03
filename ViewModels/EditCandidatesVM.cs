using System.ComponentModel.DataAnnotations;

namespace TawtheefTest.ViewModels
{
  public class EditCandidatesVM
  {
    [Required(ErrorMessage = "الرجاء ادخال الرقم التعريفي")]
    public int Id { get; set; }
    [Required(ErrorMessage = "الرجاء ادخال الاسم")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "الاسم يجب أن يكون أطول من 3 أحرف")]
    public string Name { get; set; }
    [Required(ErrorMessage = "الرجاء ادخال الرقم التعريفي")]
    [Phone(ErrorMessage = "الرجاء ادخال رقم الهاتف بشكل صحيح")]
    [StringLength(8, MinimumLength = 8, ErrorMessage = "الرجاء ادخال رقم الهاتف بشكل صحيح")]
    public string PhoneNumber { get; set; }
    [Required(ErrorMessage = "الرجاء اختيار الوظيفة")]
    public int JobId { get; set; }

  }
}
