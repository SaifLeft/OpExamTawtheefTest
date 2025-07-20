using TawtheefTest.DTOs;

namespace TawtheefTest.ViewModels
{
    /// <summary>
    /// نموذج عرض ترتيب المرشحين في الاختبار
    /// </summary>
    public class ExamRankingsViewModel
    {
        public long ExamId { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public string JobName { get; set; } = string.Empty;
        public List<CandidateRankingDTO> Rankings { get; set; } = new List<CandidateRankingDTO>();
        public ExamStatisticsDTO Statistics { get; set; } = new ExamStatisticsDTO();
    }

    /// <summary>
    /// نموذج عرض تقييم مرشح محدد
    /// </summary>
    public class CandidateEvaluationViewModel
    {
        public long CandidateExamId { get; set; }
        public string CandidateName { get; set; } = string.Empty;
        public string ExamName { get; set; } = string.Empty;
        public string JobName { get; set; } = string.Empty;
        public ExamEvaluationResultDTO EvaluationResult { get; set; } = new ExamEvaluationResultDTO();
        public long RankPosition { get; set; }
    }

    /// <summary>
    /// نموذج عرض مقارنة النتائج
    /// </summary>
    public class ResultsComparisonViewModel
    {
        public List<CandidateEvaluationViewModel> Candidates { get; set; } = new List<CandidateEvaluationViewModel>();
        public string ExamName { get; set; } = string.Empty;
        public string JobName { get; set; } = string.Empty;
    }

    /// <summary>
    /// نموذج عرض إحصائيات مفصلة
    /// </summary>
    public class DetailedStatisticsViewModel
    {
        public long ExamId { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public string JobName { get; set; } = string.Empty;
        public ExamStatisticsDTO Statistics { get; set; } = new ExamStatisticsDTO();
        public DifficultyBreakdownDTO DifficultyBreakdown { get; set; } = new DifficultyBreakdownDTO();
        public List<CandidateRankingDTO> TopPerformers { get; set; } = new List<CandidateRankingDTO>();
    }
}
