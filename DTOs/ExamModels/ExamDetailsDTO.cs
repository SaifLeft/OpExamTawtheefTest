using System;
using System.Collections.Generic;
using TawtheefTest.DTOs;
using TawtheefTest.Enums;

namespace TawtheefTest.DTOs.ExamModels
{
  public class ExamDetailsDTO
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public int JobId { get; set; }
    public string JobName { get; set; }
    public int QuestionCount { get; set; }
    public int OptionsCount { get; set; }
    public string QuestionType { get; set; }
    public string Difficulty { get; set; }
    public string Description { get; set; }
    public int Duration { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ExamStartDate { get; set; }
    public DateTime ExamEndDate { get; set; }
    public bool ShowResultsImmediately { get; set; }
    public bool SendExamLinkToApplicants { get; set; }
    public ExamStatus Status { get; set; }
    public List<QuestionSetDto> QuestionSets { get; set; }
  }

}
