using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using TawtheefTest.Enum;

namespace TawtheefTest.ViewModels
{
  public class OpExamQuestionSetViewModel
  {

    [Required(ErrorMessage = "الرجاء إدخال اسم مجموعة الأسئلة")]
    [Display(Name = "اسم مجموعة الأسئلة")]
    public string Name { get; set; }

    [Display(Name = "الوصف")]
    public string Description { get; set; }

    [Required(ErrorMessage = "الرجاء اختيار نوع الأسئلة")]
    [Display(Name = "نوع الأسئلة")]
    public string QuestionType { get; set; }

    [Required(ErrorMessage = "الرجاء اختيار نوع مصدر المحتوى")]
    [Display(Name = "نوع مصدر المحتوى")]
    public string ContentSourceType { get; set; }

    [Display(Name = "المحتوى النصي")]
    public string TextContent { get; set; }

    [Display(Name = "الملف")]
    public IFormFile File { get; set; }

    [Range(1, 100, ErrorMessage = "يجب أن يكون عدد الأسئلة بين 1 و 100")]
    [Display(Name = "عدد الأسئلة")]
    public int QuestionCount { get; set; } = 10;

    [Required(ErrorMessage = "الرجاء اختيار مستوى الصعوبة")]
    [Display(Name = "مستوى الصعوبة")]
    public string Difficulty { get; set; } = "auto";

    [Display(Name = "الاختبار")]
    public int ExamId { get; set; }

    [Display(Name = "اللغة")]
    public string Language { get; set; } = "Arabic";

    // خصائص إضافية للتعامل مع نموذج QuestionSetsController
    public string? Topic { get; set; }
    public string? LinkUrl { get; set; }
    public string? YoutubeUrl { get; set; }
    public string? FileReference { get; set; }
    public int? OptionsCount { get; set; }
    public int? NumberOfRows { get; set; }
    public int? NumberOfCorrectOptions { get; set; }
  }

  // ... existing code ...
}
