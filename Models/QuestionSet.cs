using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TawtheefTest.Data.Structure;

namespace TawtheefTest.Models
{
  public class QuestionSet
  {
    public int Id { get; set; }

    [Required]
    [Display(Name = "اسم المجموعة")]
    public string Name { get; set; }

    [Display(Name = "وصف المجموعة")]
    public string Description { get; set; }

    [Required]
    [Display(Name = "نوع الأسئلة")]
    public QuestionType QuestionType { get; set; }

    [Required]
    [Display(Name = "الاختبار")]
    public int ExamId { get; set; }

    public Exam Exam { get; set; }
    public ICollection<Question> Questions { get; set; }
  }

  public enum QuestionType
  {
    MultipleChoice,
    TrueFalse,
    ShortAnswer,
    Essay
  }
}
