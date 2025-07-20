using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using TawtheefTest.DTOs.ExamModels;

namespace TawtheefTest.ViewModels
{
    public class ExamViewModel
    {
        public ExamDTO Exam { get; set; }
        public IEnumerable<SelectListItem> Jobs { get; set; }
        public IEnumerable<SelectListItem> QuestionTypes { get; set; }
        public IEnumerable<SelectListItem> DifficultiesTypes { get; set; }
    }

    public class ExamListViewModel
    {
        public IEnumerable<ExamDTO> Exams { get; set; }
        public string JobName { get; set; }
        public long? JobId { get; set; }
    }
}