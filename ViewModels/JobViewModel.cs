using System;
using TawtheefTest.DTOs.ExamModels;

namespace TawtheefTest.ViewModels
{
  public class JobViewModel
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public DateTime CreatedDate { get; set; }
    public int CandidateCount { get; set; }
    public int ExamCount { get; set; }
  }
}
