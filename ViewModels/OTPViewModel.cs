using System.ComponentModel.DataAnnotations;

namespace TawtheefTest.ViewModels
{
    public class OTPRequestViewModel
    {
        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [RegularExpression(@"^\d+$", ErrorMessage = "يجب أن يكون رقم الهاتف أرقامًا فقط")]
        [Display(Name = "رقم الهاتف")]
        public long PhoneNumber { get; set; }
    }

    public class OTPVerificationViewModel
    {
        public long PhoneNumber { get; set; }

        [Required(ErrorMessage = "رمز التحقق مطلوب")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "يجب أن يتكون رمز التحقق من 6 أرقام")]
        [RegularExpression(@"^\d+$", ErrorMessage = "يجب أن يكون رمز التحقق أرقامًا فقط")]
        [Display(Name = "رمز التحقق")]
        public string OTPCode { get; set; }
    }
}
