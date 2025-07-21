using TawtheefTest.Data.Structure;
using TawtheefTest.Enums;

namespace TawtheefTest.ViewModels
{
  public class ExamDetailsDto
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int JobId { get; set; }
    public string JobName { get; set; }
    public long? Duration { get; set; }
    public ExamStatus Status { get; set; }
  }
}
