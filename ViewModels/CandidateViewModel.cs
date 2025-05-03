using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using TawtheefTest.DTOs.ExamModels;
using System;
using TawtheefTest.Enum;

namespace TawtheefTest.ViewModels
{
  public class CandidateViewModel
  {
    public CandidateDTO Candidate { get; set; }
    public IEnumerable<SelectListItem> Jobs { get; set; }

    // خصائص مباشرة للوصول السهل
    public string Name => Candidate?.Name;
    public string JobName => Candidate?.JobName;
    public int? JobId => Candidate?.JobId;
  }

  public class CandidateListViewModel
  {
    public IEnumerable<CandidateDTO> Candidates { get; set; }
    public string JobName { get; set; }
    public int? JobId { get; set; }
  }

  public class CandidateExamViewModel
  {
    public int Id { get; set; }
    public int CandidateId { get; set; }
    public string CandidateName { get; set; }
    public int ExamId { get; set; }
    public string ExamName { get; set; }
    public string JobName { get; set; }
    public string Status { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal? Score { get; set; }
    public bool IsCompleted { get; set; }
    public string QuestionType { get; set; }
  }

  public class ExamForCandidateViewModel
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int JobId { get; set; }
    public string JobName { get; set; }
    public string QuestionType { get; set; }
    public string Difficulty { get; set; }
    public int QuestionCount { get; set; }
    public int Duration { get; set; }
    public bool ShowResultsImmediately { get; set; }
  }
}
