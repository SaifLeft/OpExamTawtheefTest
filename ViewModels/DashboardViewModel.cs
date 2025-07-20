using System.Collections.Generic;

namespace TawtheefTest.ViewModels
{
  public class DashboardViewModel
  {
    public long JobsCount { get; set; }
    public long CandidatesCount { get; set; }
    public long ExamsCount { get; set; }
    public long QuestionSetsCount { get; set; }
    public long CompletedExamsCount { get; set; }

    public List<JobViewModel> RecentJobs { get; set; }
    public List<CandidateViewModel> RecentCandidates { get; set; }
  }
}
