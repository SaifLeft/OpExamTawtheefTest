using System.ComponentModel;

namespace TawtheefTest.Enum
{

  // Enums
  public enum QuestionSetStatus
  {
    Pending,
    Processing,
    Completed,
    Failed
  }
  public enum ContentSourceStatus
  {
    Pending,
    Processing,
    Completed,
    Failed
  }

  public enum ContentSourceType
  {
    Topic,
    Text,
    Link,
    Youtube,
    Document,
    Image,
    Audio,
    Video
  }

  public enum ExamStatus
  {
    [Description("قيد الاعداد")]
    Draft,
    [Description("تم نشر الاختبار")]
    Published,
    [Description("مكتمل")]
    Archived
  }

  public enum CandidateExamStatus
  {
    NotStarted,
    InProgress,
    Completed,
    Abandoned
  }

  public enum QuestionSetLanguage
  {
    Arabic,
    English,
  }

  public enum QuestionTypeEnum
  {
    MCQ,
    TF,
    Open,
    FillInTheBlank,
    Ordering,
    Matching,
    MultiSelect,
    ShortAnswer
  }

  public enum FileType
  {
    Document,
    Image,
    Audio,
    Video,
    Other
  }
}
