using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TawtheefTest.DTOs;

namespace TawtheefTest.ViewModels
{
  public class QuestionSetCreateViewModel
  {
    [Required(ErrorMessage = "اسم مجموعة الأسئلة مطلوب")]
    [StringLength(100, ErrorMessage = "يجب ألا يتجاوز الاسم 100 حرف")]
    public string Name { get; set; }

    [StringLength(500, ErrorMessage = "يجب ألا يتجاوز الوصف 500 حرف")]
    public string Description { get; set; }

    [Required(ErrorMessage = "نوع الأسئلة مطلوب")]
    public string QuestionType { get; set; }

    [Required(ErrorMessage = "مستوى الصعوبة مطلوب")]
    public string Difficulty { get; set; } = "medium";

    [Required(ErrorMessage = "عدد الأسئلة مطلوب")]
    [Range(1, 100, ErrorMessage = "يجب أن يكون عدد الأسئلة بين 1 و 100")]
    public long QuestionCount { get; set; } = 10;

    [Range(2, 10, ErrorMessage = "يجب أن يكون عدد الخيارات بين 2 و 10")]
    public long? OptionsCount { get; set; } = 4;

    [Required(ErrorMessage = "موضوع الأسئلة مطلوب")]
    public string Topic { get; set; }

    public string ContentSourceType { get; set; } = "Topic";
  }

  public class ShuffleOptionsViewModel
  {
    [Required(ErrorMessage = "مجموعة الأسئلة مطلوبة")]
    public long QuestionSetId { get; set; }

    [Required(ErrorMessage = "نوع الخلط مطلوب")]
    public ShuffleType ShuffleType { get; set; } = ShuffleType.OptionsOnly;
  }

  public enum ShuffleType
  {
    [Display(Name = "خلط الخيارات فقط")]
    OptionsOnly = 1,

    [Display(Name = "خلط الأسئلة فقط")]
    QuestionsOnly = 2,

    [Display(Name = "خلط الأسئلة والخيارات")]
    Both = 3
  }

  public class MergeQuestionSetsViewModel
  {
    public List<int> SelectedIds { get; set; }
    public string MergedName { get; set; }
    public string MergedType { get; set; }
    public string MergedDifficulty { get; set; }
    public string MergedLanguage { get; set; }
    public long ShuffleQuestions { get; set; }
    public Dictionary<int, int> QuestionsCountPerSet { get; set; }
  }
}
