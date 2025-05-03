using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using TawtheefTest.DTOs.ExamModels;

namespace TawtheefTest.ViewModels
{
    public class CandidateViewModel
    {
        public CandidateDTO Candidate { get; set; }
        public IEnumerable<SelectListItem> Jobs { get; set; }
    }

    public class CandidateListViewModel
    {
        public IEnumerable<CandidateDTO> Candidates { get; set; }
        public string JobName { get; set; }
        public int? JobId { get; set; }
    }
}