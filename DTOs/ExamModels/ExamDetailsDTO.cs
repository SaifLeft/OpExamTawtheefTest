using System;
using System.Collections.Generic;
using TawtheefTest.DTOs;

namespace TawtheefTest.DTOs.ExamModels
{
    public class ExamDetailsDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int JobId { get; set; }
        public string Description { get; set; }
        public int Duration { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ExamStartDate { get; set; }
        public DateTime ExamEndDate { get; set; }
        public bool ShowResultsImmediately { get; set; }
        public bool SendExamLinkToApplicants { get; set; }
        public List<QuestionSetDto> QuestionSets { get; set; }
    }
}