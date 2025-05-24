using TawtheefTest.Data.Structure;
using TawtheefTest.DTOs;
using Microsoft.EntityFrameworkCore;

namespace TawtheefTest.Services
{
    public class ExamEvaluationService
    {
        private readonly ApplicationDbContext _context;

        public ExamEvaluationService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// حساب النقاط المخصصة للسؤال حسب مستوى صعوبته
        /// </summary>
        /// <param name="difficultyLevel">مستوى الصعوبة</param>
        /// <returns>عدد النقاط</returns>
        public int GetPointsForDifficulty(string difficultyLevel)
        {
            return difficultyLevel?.ToLower() switch
            {
                "easy" => 1,    // الأسئلة السهلة = 1 نقطة
                "medium" => 2,  // الأسئلة المتوسطة = 2 نقاط
                "hard" => 3,    // الأسئلة الصعبة = 3 نقاط
                _ => 1          // القيمة الافتراضية
            };
        }

        /// <summary>
        /// تحديث نقاط الأسئلة الموجودة حسب صعوبة مجموعة الأسئلة
        /// </summary>
        /// <param name="examId">معرف الاختبار</param>
        public async Task UpdateQuestionPointsAsync(int examId)
        {
            var questions = await _context.Questions
                .Include(q => q.QuestionSet)
                .Where(q => q.QuestionSet.ExamQuestionSets.Any(eqs => eqs.ExamId == examId))
                .ToListAsync();

            foreach (var question in questions)
            {
                // إذا لم يتم تحديد صعوبة للسؤال، استخدم صعوبة مجموعة الأسئلة
                var difficulty = question.DifficultyLevel ?? question.QuestionSet.Difficulty;
                question.DifficultyLevel = difficulty;
                question.Points = GetPointsForDifficulty(difficulty);
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// حساب النتيجة المحسنة لاختبار المرشح
        /// </summary>
        /// <param name="candidateExamId">معرف اختبار المرشح</param>
        /// <returns>معلومات النتيجة المحسنة</returns>
        public async Task<ExamEvaluationResultDTO> CalculateEnhancedScoreAsync(int candidateExamId)
        {
            var candidateExam = await _context.CandidateExams
                .Include(ce => ce.CandidateAnswers)
                    .ThenInclude(ca => ca.Question)
                .Include(ce => ce.Exam)
                    .ThenInclude(e => e.ExamQuestionSets)
                        .ThenInclude(eqs => eqs.QuestionSet)
                            .ThenInclude(qs => qs.Questions)
                .FirstOrDefaultAsync(ce => ce.Id == candidateExamId);

            if (candidateExam == null)
            {
                throw new InvalidOperationException("اختبار المرشح غير موجود");
            }

            // تحديث نقاط الأسئلة إذا لم تكن محدثة
            await UpdateQuestionPointsAsync(candidateExam.ExamId);

            // جمع جميع الأسئلة من الاختبار
            var allQuestions = candidateExam.Exam.ExamQuestionSets
                .SelectMany(eqs => eqs.QuestionSet.Questions)
                .ToList();

            // حساب أقصى نقاط ممكنة
            int maxPossiblePoints = allQuestions.Sum(q => q.Points);

            // حساب النقاط المحققة
            int totalPointsEarned = 0;
            int easyCorrect = 0, mediumCorrect = 0, hardCorrect = 0;

            foreach (var answer in candidateExam.CandidateAnswers.Where(ca => ca.IsCorrect == true))
            {
                var question = allQuestions.FirstOrDefault(q => q.Id == answer.QuestionId);
                if (question != null)
                {
                    totalPointsEarned += question.Points;

                    // تصنيف الإجابات الصحيحة حسب الصعوبة
                    switch (question.DifficultyLevel?.ToLower())
                    {
                        case "easy":
                            easyCorrect++;
                            break;
                        case "medium":
                            mediumCorrect++;
                            break;
                        case "hard":
                            hardCorrect++;
                            break;
                    }
                }
            }

            // حساب النسبة المئوية
            decimal scorePercentage = maxPossiblePoints > 0 ?
                (decimal)totalPointsEarned / maxPossiblePoints * 100 : 0;

            // حساب مدة الإكمال
            TimeSpan? completionDuration = null;
            if (candidateExam.StartTime.HasValue && candidateExam.EndTime.HasValue)
            {
                completionDuration = candidateExam.EndTime.Value - candidateExam.StartTime.Value;
            }

            // تحديث بيانات اختبار المرشح
            candidateExam.TotalPoints = totalPointsEarned;
            candidateExam.MaxPossiblePoints = maxPossiblePoints;
            candidateExam.EasyQuestionsCorrect = easyCorrect;
            candidateExam.MediumQuestionsCorrect = mediumCorrect;
            candidateExam.HardQuestionsCorrect = hardCorrect;
            candidateExam.CompletionDuration = completionDuration;
            candidateExam.Score = scorePercentage;

            await _context.SaveChangesAsync();

            return new ExamEvaluationResultDTO
            {
                CandidateExamId = candidateExamId,
                TotalPointsEarned = totalPointsEarned,
                MaxPossiblePoints = maxPossiblePoints,
                ScorePercentage = scorePercentage,
                EasyQuestionsCorrect = easyCorrect,
                MediumQuestionsCorrect = mediumCorrect,
                HardQuestionsCorrect = hardCorrect,
                CompletionDuration = completionDuration,
                TotalAnsweredQuestions = candidateExam.CandidateAnswers.Count(ca => ca.IsCorrect.HasValue),
                TotalCorrectAnswers = candidateExam.CandidateAnswers.Count(ca => ca.IsCorrect == true)
            };
        }

        /// <summary>
        /// ترتيب المرشحين في الاختبار حسب المعايير المحددة
        /// </summary>
        /// <param name="examId">معرف الاختبار</param>
        /// <returns>قائمة مرتبة من المرشحين</returns>
        public async Task<List<CandidateRankingDTO>> RankCandidatesAsync(int examId)
        {
            var candidateExams = await _context.CandidateExams
                .Include(ce => ce.Candidate)
                .Where(ce => ce.ExamId == examId && ce.Status == "Completed")
                .ToListAsync();

            // ترتيب المرشحين حسب:
            // 1. إجمالي النقاط (تنازلي)
            // 2. مدة الإكمال (تصاعدي)
            var rankedCandidates = candidateExams
                .OrderByDescending(ce => ce.TotalPoints)
                .ThenBy(ce => ce.CompletionDuration?.TotalMinutes ?? double.MaxValue)
                .Select((ce, index) => new CandidateRankingDTO
                {
                    Rank = index + 1,
                    CandidateId = ce.CandidateId,
                    CandidateName = ce.Candidate.Name,
                    TotalPoints = ce.TotalPoints,
                    MaxPossiblePoints = ce.MaxPossiblePoints,
                    ScorePercentage = ce.Score ?? 0,
                    CompletionTimeMinutes = ce.CompletionDuration?.TotalMinutes ?? 0,
                    EasyCorrect = ce.EasyQuestionsCorrect,
                    MediumCorrect = ce.MediumQuestionsCorrect,
                    HardCorrect = ce.HardQuestionsCorrect
                })
                .ToList();

            // تحديث ترتيب المرشحين في قاعدة البيانات
            foreach (var candidate in rankedCandidates)
            {
                var candidateExam = candidateExams.First(ce => ce.CandidateId == candidate.CandidateId);
                candidateExam.RankPosition = candidate.Rank;
            }

            await _context.SaveChangesAsync();

            return rankedCandidates;
        }

        /// <summary>
        /// الحصول على إحصائيات تفصيلية للاختبار
        /// </summary>
        /// <param name="examId">معرف الاختبار</param>
        /// <returns>إحصائيات الاختبار</returns>
        public async Task<ExamStatisticsDTO> GetExamStatisticsAsync(int examId)
        {
            var candidateExams = await _context.CandidateExams
                .Where(ce => ce.ExamId == examId && ce.Status == "Completed")
                .ToListAsync();

            if (!candidateExams.Any())
            {
                return new ExamStatisticsDTO
                {
                    ExamId = examId,
                    TotalCandidates = 0
                };
            }

            var totalCandidates = candidateExams.Count;
            var averageScore = candidateExams.Average(ce => ce.Score ?? 0);
            var averageCompletionTime = candidateExams
                .Where(ce => ce.CompletionDuration.HasValue)
                .Average(ce => ce.CompletionDuration.Value.TotalMinutes);

            var difficultyBreakdown = new
            {
                Easy = candidateExams.Average(ce => ce.EasyQuestionsCorrect),
                Medium = candidateExams.Average(ce => ce.MediumQuestionsCorrect),
                Hard = candidateExams.Average(ce => ce.HardQuestionsCorrect)
            };

            return new ExamStatisticsDTO
            {
                ExamId = examId,
                TotalCandidates = totalCandidates,
                AverageScore = Math.Round(averageScore, 2),
                AverageCompletionTimeMinutes = Math.Round(averageCompletionTime, 2),
                AverageEasyQuestionsCorrect = Math.Round(difficultyBreakdown.Easy, 2),
                AverageMediumQuestionsCorrect = Math.Round(difficultyBreakdown.Medium, 2),
                AverageHardQuestionsCorrect = Math.Round(difficultyBreakdown.Hard, 2),
                HighestScore = candidateExams.Max(ce => ce.Score ?? 0),
                LowestScore = candidateExams.Min(ce => ce.Score ?? 0)
            };
        }
    }
}
