using System.Collections.Generic;

namespace TawtheefTest.ViewModels
{
  public class DashboardViewModel
  {
    public int JobsCount { get; set; }
    public int CandidatesCount { get; set; }
    public int ExamsCount { get; set; }
    public int QuestionSetsCount { get; set; }
    public int CompletedExamsCount { get; set; }

    public List<JobViewModel> RecentJobs { get; set; }
    public List<CandidateViewModel> RecentCandidates { get; set; }
  }
}
