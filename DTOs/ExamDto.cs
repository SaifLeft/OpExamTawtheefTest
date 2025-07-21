using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TawtheefTest.Data.Structure;

namespace TawtheefTest.DTOs
{
  public class ExamDto
  {
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم الاختبار مطلوب")]
    [Display(Name = "اسم الاختبار")]
    public string Name { get; set; }

    [Required(ErrorMessage = "الوظيفة مطلوبة")]
    [Display(Name = "الوظيفة")]
    public int JobId { get; set; }

    [Display(Name = "اسم الوظيفة")]
    public string JobName { get; set; }

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
    public int ShowResultImmediately { get; set; }

    [Display(Name = "إرسال روابط الاختبار للمتقدمين")]
    public int SendExamLinks { get; set; }

    [Display(Name = "تاريخ الإنشاء")]
    public DateTime CreatedDate { get; set; }

    [Display(Name = "عدد المتقدمين")]
    public long? CandidatesCount { get; set; }

    // مجموعات الأسئلة المرتبطة بالاختبار
    public List<QuestionSetDto> QuestionSets { get; set; }
  }

  /// <summary>
  /// DTO لصفحة تعليمات الامتحان قبل البدء
  /// </summary>
  public class ExamInstructionsDto
  {
    public int ExamId { get; set; }
    public string ExamName { get; set; }
    public string ExamDescription { get; set; }
    public string JobName { get; set; }
    public int Duration { get; set; } // مدة الامتحان بالدقائق
    public int TotalQuestions { get; set; } // إجمالي عدد الأسئلة
    public decimal? PassPercentage { get; set; } // نسبة النجاح المطلوبة
    public string CandidateName { get; set; }
    public List<string> ExamRules { get; set; } = new List<string>(); // قواعد الامتحان
    public List<string> TechnicalInstructions { get; set; } = new List<string>(); // التعليمات التقنية
    public int HasExistingAttempt { get; set; } // هل يوجد محاولة سابقة
    public long? ExistingAttemptId { get; set; } // معرف المحاولة السابقة إن وجدت
  }
}
