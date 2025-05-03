using System;
using System.Collections.Generic;

namespace TawtheefTest.Models
{
  public class Job
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; } // 8-digit random code
    public DateTime CreatedDate { get; set; }

    // Navigation properties
    public ICollection<Candidate> Candidates { get; set; }
    public ICollection<Exam> Exams { get; set; }
  }
}
