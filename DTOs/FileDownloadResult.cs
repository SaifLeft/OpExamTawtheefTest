using TawtheefTest.Data.Structure;
using TawtheefTest.Enums;

namespace TawtheefTest.DTOs
{
  // Helper class for file download result
  public class FileDownloadResult
  {
    public QuestionSet QuestionSet { get; set; }
    public ContentSourceType ContentSourceType { get; set; }
  }
}
