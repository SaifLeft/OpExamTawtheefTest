using TawtheefTest.Enums;

namespace TawtheefTest.DTOs.ExamModels
{
  public class ExamCandidateDTO
  {
    public long Id { get; set; }
    public long CandidateId { get; set; }
    public string Name { get; set; }
    public long Phone { get; set; }
    public string? Score { get; set; } = 0;
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public CandidateExamStatus Status { get; set; }
    public long SequenceNumber { get; set; }
    public bool HasPassed { get; set; } = false;
  }
}
