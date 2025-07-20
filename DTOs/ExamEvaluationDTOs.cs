namespace TawtheefTest.DTOs
{
    /// <summary>
    /// نتيجة تقييم الاختبار المحسن
    /// </summary>
    public class ExamEvaluationResultDTO
    {
        public long CandidateExamId { get; set; }
        public long TotalPointsEarned { get; set; }
        public long MaxPossiblePoints { get; set; }
        public decimal ScorePercentage { get; set; }
        public long EasyQuestionsCorrect { get; set; }
        public long MediumQuestionsCorrect { get; set; }
        public long HardQuestionsCorrect { get; set; }
        public TimeSpan? CompletionDuration { get; set; }
        public long TotalAnsweredQuestions { get; set; }
        public long TotalCorrectAnswers { get; set; }
    }

    /// <summary>
    /// ترتيب المرشحين
    /// </summary>
    public class CandidateRankingDTO
    {
        public long Rank { get; set; }
        public long CandidateId { get; set; }
        public string CandidateName { get; set; } = string.Empty;
        public long TotalPoints { get; set; }
        public long MaxPossiblePoints { get; set; }
        public decimal ScorePercentage { get; set; }
        public double CompletionTimeMinutes { get; set; }
        public long EasyCorrect { get; set; }
        public long MediumCorrect { get; set; }
        public long HardCorrect { get; set; }
    }

    /// <summary>
    /// إحصائيات الاختبار
    /// </summary>
    public class ExamStatisticsDTO
    {
        public long ExamId { get; set; }
        public long TotalCandidates { get; set; }
        public decimal AverageScore { get; set; }
        public double AverageCompletionTimeMinutes { get; set; }
        public double AverageEasyQuestionsCorrect { get; set; }
        public double AverageMediumQuestionsCorrect { get; set; }
        public double AverageHardQuestionsCorrect { get; set; }
        public decimal HighestScore { get; set; }
        public decimal LowestScore { get; set; }
    }

    /// <summary>
    /// تفاصيل النقاط حسب الصعوبة
    /// </summary>
    public class DifficultyBreakdownDTO
    {
        public long EasyQuestions { get; set; }
        public long EasyCorrect { get; set; }
        public long EasyPoints { get; set; }

        public long MediumQuestions { get; set; }
        public long MediumCorrect { get; set; }
        public long MediumPoints { get; set; }

        public long HardQuestions { get; set; }
        public long HardCorrect { get; set; }
        public long HardPoints { get; set; }

        public long TotalPoints { get; set; }
        public long MaxPossiblePoints { get; set; }
    }
}
