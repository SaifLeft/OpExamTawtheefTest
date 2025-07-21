using TawtheefTest.Enums;

namespace TawtheefTest.Services.Exams
{
  // IExamUtilityService.cs
  public interface IExamUtilityService
  {
    string GetQuestionTypeName(QuestionTypeEnum type);
    string GetDifficultyName(string difficulty);
    string GetStatusDescription(QuestionSetStatus status);
  }

  // ExamUtilityService.cs
  public class ExamUtilityService : IExamUtilityService
  {
    public string GetQuestionTypeName(QuestionTypeEnum type)
    {
      return type switch
      {
        QuestionTypeEnum.MCQ => "اختيار من متعدد",
        QuestionTypeEnum.TF => "صح/خطأ",
        QuestionTypeEnum.Open => "إجابة مفتوحة",
        QuestionTypeEnum.FillInTheBlank => "ملء الفراغات",
        QuestionTypeEnum.Ordering => "ترتيب",
        QuestionTypeEnum.Matching => "مطابقة",
        QuestionTypeEnum.MultiSelect => "اختيار متعدد",
        QuestionTypeEnum.ShortAnswer => "إجابة قصيرة",
        _ => type.ToString()
      };
    }

    public string GetDifficultyName(string difficulty)
    {
      return difficulty switch
      {
        "easy" => "سهل",
        "medium" => "متوسط",
        "hard" => "صعب",
        _ => difficulty
      };
    }

    public string GetStatusDescription(QuestionSetStatus status)
    {
      return status switch
      {
        QuestionSetStatus.Pending => "في الانتظار",
        QuestionSetStatus.Processing => "قيد المعالجة",
        QuestionSetStatus.Completed => "مكتمل",
        QuestionSetStatus.Failed => "فشل",
        _ => status.ToString()
      };
    }
  }
}
