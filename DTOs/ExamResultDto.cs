using System;
using System.ComponentModel.DataAnnotations;

namespace TawtheefTest.DTOs
{
  public class ExamResultDto
  {
    public int Id { get; set; }

    [Display(Name = "الاختبار")]
    public int ExamId { get; set; }

    [Display(Name = "اسم المتقدم")]
    public string ApplicantName { get; set; }

    [Display(Name = "البريد الإلكتروني")]
    public string ApplicantEmail { get; set; }

    [Display(Name = "وقت البدء")]
    public DateTime? StartTime { get; set; }

    [Display(Name = "وقت الانتهاء")]
    public DateTime? EndTime { get; set; }

    [Display(Name = "النتيجة")]
    public decimal? Score { get; set; }

    [Display(Name = "الحالة")]
    public string Status { get; set; }
  }
}
