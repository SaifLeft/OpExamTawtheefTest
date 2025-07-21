namespace TawtheefTest.DTOs
{
    /// <summary>
    /// نتيجة تقييم الاختبار المحسن
    /// </summary>
    public class ExamEvaluationResultDTO
    {
        public int CandidateExamId { get; set; }
        public int TotalPointsEarned { get; set; }
        public int MaxPossiblePoints { get; set; }
        public decimal ScorePercentage { get; set; }
        public int EasyQuestionsCorrect { get; set; }
        public int MediumQuestionsCorrect { get; set; }
        public int HardQuestionsCorrect { get; set; }
        public TimeSpan? CompletionDuration { get; set; }
        public int TotalAnsweredQuestions { get; set; }
        public int TotalCorrectAnswers { get; set; }
    }

    /// <summary>
    /// ترتيب المرشحين
    /// </summary>
    public class CandidateRankingDTO
    {
        public int Rank { get; set; }
        public int CandidateId { get; set; }
        public string CandidateName { get; set; } = string.Empty;
        public int TotalPoints { get; set; }
        public int MaxPossiblePoints { get; set; }
        public decimal ScorePercentage { get; set; }
        public double CompletionTimeMinutes { get; set; }
        public int EasyCorrect { get; set; }
        public int MediumCorrect { get; set; }
        public int HardCorrect { get; set; }
    }

    /// <summary>
    /// إحصائيات الاختبار
    /// </summary>
    public class ExamStatisticsDTO
    {
        public int ExamId { get; set; }
        public int TotalCandidates { get; set; }
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
        public int EasyQuestions { get; set; }
        public int EasyCorrect { get; set; }
        public int EasyPoints { get; set; }

        public int MediumQuestions { get; set; }
        public int MediumCorrect { get; set; }
        public int MediumPoints { get; set; }

        public int HardQuestions { get; set; }
        public int HardCorrect { get; set; }
        public int HardPoints { get; set; }

        public int TotalPoints { get; set; }
        public int MaxPossiblePoints { get; set; }
    }
}
