using System;

namespace TawtheefTest.DTOs.ExamModels
{
  public class CandidateDTO
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public int Phone { get; set; }
    public string PhoneNumber => Phone.ToString();
    public int JobId { get; set; }
    public string JobName { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
  }
}
