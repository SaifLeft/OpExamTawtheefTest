using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using TawtheefTest.DTOs.ExamModels;
using System;
using TawtheefTest.Enums;

namespace TawtheefTest.ViewModels
{
  public class CandidateViewModel
  {
    public long Id { get; set; }
    public string Name { get; set; }
    public long Phone { get; set; }
    public long JobId { get; set; }
    public string JobTitle { get; set; }
    public long IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string JobName => JobTitle;
  }

  public class CandidateListViewModel
  {
    public IEnumerable<CandidateDTO> Candidates { get; set; }
    public string JobName { get; set; }
    public long? JobId { get; set; }
  }

  public class CreateCandidatesVM
  {
    public string Name { get; set; }
    public long Phone { get; set; }
    public long JobId { get; set; }
  }

  public class EditCandidatesVM
  {
    public long Id { get; set; }
    public string Name { get; set; }
    public string PhoneNumber { get; set; }
    public long JobId { get; set; }
  }
}
