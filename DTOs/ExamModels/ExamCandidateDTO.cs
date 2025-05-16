using TawtheefTest.Enums;

namespace TawtheefTest.DTOs.ExamModels
{
  public class ExamCandidateDTO
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public int Phone { get; set; }
    public int Score { get; set; } = 0;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public CandidateExamStatus Status { get; set; }
    public int SequenceNumber { get; set; }
    public bool HasPassed { get; set; } = false;
  }
}
