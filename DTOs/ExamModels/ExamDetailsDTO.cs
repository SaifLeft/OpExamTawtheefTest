using System;
using System.Collections.Generic;
using TawtheefTest.DTOs;
using TawtheefTest.Enums;

namespace TawtheefTest.DTOs.ExamModels
{
  public class ExamDetailsDTO
  {
    public long Id { get; set; }
    public string Name { get; set; }
    public long JobId { get; set; }
    public string JobName { get; set; }
    public long TotalQuestionsPerCandidate { get; set; }
    public long OptionsCount { get; set; }
    public string QuestionType { get; set; }
    public string Difficulty { get; set; }
    public string Description { get; set; }
    public long Duration { get; set; }
    public string CreatedDate { get; set; }
    public string ExamStartDate { get; set; }
    public string ExamEndDate { get; set; }
    public long ShowResultsImmediately { get; set; }
    public long SendExamLinkToApplicants { get; set; }
    public ExamStatus Status { get; set; }
    public List<QuestionSetDto> QuestionSets { get; set; }
    public List<ExamCandidateDTO> Candidates { get; set; }
  }

}
