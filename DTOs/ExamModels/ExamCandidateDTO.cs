using TawtheefTest.Enums;

namespace TawtheefTest.DTOs.ExamModels
{
  public class ExamCandidateDTO
  {
    public int Id { get; set; }
    public int CandidateId { get; set; }
    public string Name { get; set; }
    public int Phone { get; set; }
    public decimal? Score { get; set; } = 0;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public AssignmentStatus Status { get; set; }
    public int SequenceNumber { get; set; }
    public bool HasPassed { get; set; } = false;
  }
}
