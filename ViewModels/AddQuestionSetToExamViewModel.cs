using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TawtheefTest.Enums;

namespace TawtheefTest.ViewModels
{
  public class AddQuestionSetToExamViewModel
  {
    public int QuestionSetId { get; set; }

    [Display(Name = "اسم المجموعة")]
    public string QuestionSetName { get; set; }

    [Display(Name = "نوع الأسئلة")]
    public string QuestionType { get; set; }

    [Display(Name = "اللغة")]
    public QuestionSetLanguage Language { get; set; }

    [Display(Name = "عدد الأسئلة")]
    public int QuestionCount { get; set; }

    [Display(Name = "الاختبار")]
    [Required(ErrorMessage = "يرجى اختيار اختبار")]
    public int ExamId { get; set; }

    [Display(Name = "ترتيب العرض")]
    [Range(1, 100, ErrorMessage = "يجب أن يكون ترتيب العرض بين 1 و 100")]
    public int DisplayOrder { get; set; } = 1;

    public List<ExamSummaryViewModel> AvailableExams { get; set; } = new List<ExamSummaryViewModel>();

    public List<ExamSummaryViewModel> AssignedExams { get; set; } = new List<ExamSummaryViewModel>();
  }

  public class ExamSummaryViewModel
  {
    public int Id { get; set; }

    public string Name { get; set; }

    public string JobTitle { get; set; }

    public ExamStatus Status { get; set; }
  }
}
