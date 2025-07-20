using System;
using System.ComponentModel.DataAnnotations;

namespace TawtheefTest.ViewModels
{
  public class PublishExamViewModel
  {
    public long ExamId { get; set; }

    [Display(Name = "اسم الاختبار")]
    public string ExamName { get; set; }

    [Display(Name = "الوظيفة")]
    public string JobName { get; set; }

    [Display(Name = "تاريخ بدء الاختبار")]
    [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd HH:mm}")]
    public DateTime StartDate { get; set; }

    [Display(Name = "تاريخ انتهاء الاختبار")]
    [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd HH:mm}")]
    public DateTime EndDate { get; set; }

    [Display(Name = "إرسال إشعار SMS للمتقدمين")]
    public bool SendSmsNotification { get; set; }

    [Display(Name = "عدد المتقدمين المؤهلين")]
    public long ApplicantsCount { get; set; }

    [Display(Name = "نص رسالة الإشعار")]
    public string NotificationText { get; set; }
  }
}
