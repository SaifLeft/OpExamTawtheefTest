using System;
using System.Collections.Generic;

namespace TawtheefTest.Models
{
    public class Candidate
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public int JobId { get; set; }
        public DateTime RegisteredDate { get; set; }

        // Navigation properties
        public Job Job { get; set; }
        public ICollection<CandidateExam> CandidateExams { get; set; }
    }
}