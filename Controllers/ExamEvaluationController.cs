using Microsoft.AspNetCore.Mvc;
using TawtheefTest.Services;
using TawtheefTest.DTOs;
using TawtheefTest.Data.Structure;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.ViewModels;
using AutoMapper;

namespace TawtheefTest.Controllers
{
    public class ExamEvaluationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ExamEvaluationService _evaluationService;
        private readonly IMapper _mapper;

        public ExamEvaluationController(ApplicationDbContext context, ExamEvaluationService evaluationService, IMapper mapper)
        {
            _context = context;
            _evaluationService = evaluationService;
            _mapper = mapper;
        }

        /// <summary>
        /// صفحة ترتيب المرشحين للاختبار
        /// </summary>
        /// <param name="examId">معرف الاختبار</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Rankings(int examId)
        {
            var exam = await _context.Exams
                .Include(e => e.Job)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null)
            {
                return NotFound("الاختبار غير موجود");
            }

            var rankings = await _evaluationService.RankCandidatesAsync(examId);
            var statistics = await _evaluationService.GetExamStatisticsAsync(examId);

            var viewModel = new ExamRankingsViewModel
            {
                ExamId = examId,
                ExamName = exam.Name,
                JobName = exam.Job.Title,
                Rankings = rankings,
                Statistics = statistics
            };

            return View(viewModel);
        }

        /// <summary>
        /// API للحصول على ترتيب المرشحين
        /// </summary>
        /// <param name="examId">معرف الاختبار</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetRankings(int examId)
        {
            try
            {
                var rankings = await _evaluationService.RankCandidatesAsync(examId);
                return Json(new { success = true, data = rankings });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// API للحصول على إحصائيات الاختبار
        /// </summary>
        /// <param name="examId">معرف الاختبار</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetStatistics(int examId)
        {
            try
            {
                var statistics = await _evaluationService.GetExamStatisticsAsync(examId);
                return Json(new { success = true, data = statistics });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// صفحة تفاصيل نتيجة مرشح محدد
        /// </summary>
        /// <param name="candidateExamId">معرف اختبار المرشح</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> CandidateResult(int candidateExamId)
        {
            try
            {
                var evaluationResult = await _evaluationService.CalculateEnhancedScoreAsync(candidateExamId);

                var candidateExam = await _context.CandidateExams
                    .Include(ce => ce.Candidate)
                    .Include(ce => ce.Exam)
                        .ThenInclude(e => e.Job)
                    .FirstOrDefaultAsync(ce => ce.Id == candidateExamId);

                if (candidateExam == null)
                {
                    return NotFound("نتيجة المرشح غير موجودة");
                }

                var viewModel = new CandidateEvaluationViewModel
                {
                    CandidateExamId = candidateExamId,
                    CandidateName = candidateExam.Candidate.Name,
                    ExamName = candidateExam.Exam.Name,
                    JobName = candidateExam.Exam.Job.Title,
                    EvaluationResult = evaluationResult,
                    RankPosition = candidateExam.RankPosition
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"خطأ في جلب النتيجة: {ex.Message}";
                return RedirectToAction("Index", "CandidateExams");
            }
        }

        /// <summary>
        /// API لإعادة حساب نتائج جميع المرشحين في الاختبار
        /// </summary>
        /// <param name="examId">معرف الاختبار</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> RecalculateResults(int examId)
        {
            try
            {
                var candidateExams = await _context.CandidateExams
                    .Where(ce => ce.ExamId == examId && ce.Status == "Completed")
                    .ToListAsync();

                var results = new List<ExamEvaluationResultDTO>();

                foreach (var candidateExam in candidateExams)
                {
                    var result = await _evaluationService.CalculateEnhancedScoreAsync(candidateExam.Id);
                    results.Add(result);
                }

                // إعادة ترتيب المرشحين
                await _evaluationService.RankCandidatesAsync(examId);

                return Json(new
                {
                    success = true,
                    message = $"تم إعادة حساب نتائج {results.Count} مرشح بنجاح",
                    processedCandidates = results.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// API للمقارنة بين نتائج المرشحين
        /// </summary>
        /// <param name="candidateExamIds">معرفات اختبارات المرشحين</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> CompareCandidates([FromBody] List<int> candidateExamIds)
        {
            try
            {
                if (candidateExamIds == null || candidateExamIds.Count < 2)
                {
                    return Json(new { success = false, message = "يجب اختيار مرشحين على الأقل للمقارنة" });
                }

                var comparisons = new List<object>();

                foreach (var candidateExamId in candidateExamIds)
                {
                    var result = await _evaluationService.CalculateEnhancedScoreAsync(candidateExamId);
                    var candidateExam = await _context.CandidateExams
                        .Include(ce => ce.Candidate)
                        .FirstOrDefaultAsync(ce => ce.Id == candidateExamId);

                    if (candidateExam != null)
                    {
                        comparisons.Add(new
                        {
                            CandidateName = candidateExam.Candidate.Name,
                            TotalPoints = result.TotalPointsEarned,
                            MaxPoints = result.MaxPossiblePoints,
                            ScorePercentage = result.ScorePercentage,
                            EasyCorrect = result.EasyQuestionsCorrect,
                            MediumCorrect = result.MediumQuestionsCorrect,
                            HardCorrect = result.HardQuestionsCorrect,
                            CompletionTime = result.CompletionDuration?.TotalMinutes ?? 0,
                            Rank = candidateExam.RankPosition
                        });
                    }
                }

                return Json(new { success = true, data = comparisons });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// تصدير نتائج الاختبار إلى Excel
        /// </summary>
        /// <param name="examId">معرف الاختبار</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> ExportResults(int examId)
        {
            try
            {
                var exam = await _context.Exams
                    .Include(e => e.Job)
                    .FirstOrDefaultAsync(e => e.Id == examId);

                if (exam == null)
                {
                    return NotFound("الاختبار غير موجود");
                }

                var rankings = await _evaluationService.RankCandidatesAsync(examId);
                var statistics = await _evaluationService.GetExamStatisticsAsync(examId);

                // هنا يمكن إضافة كود تصدير Excel
                // لدواعي البساطة، سأرجع JSON
                var exportData = new
                {
                    ExamInfo = new
                    {
                        Name = exam.Name,
                        Job = exam.Job.Title,
                        TotalCandidates = statistics.TotalCandidates,
                        AverageScore = statistics.AverageScore,
                        ExportDate = DateTime.Now
                    },
                    Rankings = rankings,
                    Statistics = statistics
                };

                return Json(exportData);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
