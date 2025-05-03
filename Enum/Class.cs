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
    Draft,
    Published,
    Archived
  }

  public enum CandidateExamStatus
  {
    NotStarted,
    InProgress,
    Completed,
    Abandoned
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
