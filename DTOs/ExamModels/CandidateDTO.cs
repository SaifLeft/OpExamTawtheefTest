using System;

namespace TawtheefTest.DTOs.ExamModels
{
  public class CandidateDTO
  {
    public long Id { get; set; }
    public string Name { get; set; }
    public long Phone { get; set; }
    public string PhoneNumber => Phone.ToString();
    public long JobId { get; set; }
    public string JobName { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
  }
}
