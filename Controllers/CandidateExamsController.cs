using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using TawtheefTest.ViewModels;
using TawtheefTest.DTOs;
using TawtheefTest.Enums;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using TawtheefTest.Services;

namespace TawtheefTest.Controllers
{
  public class CandidateExamsController : Controller
  {
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;
    private readonly ExamEvaluationService _evaluationService;

    public CandidateExamsController(ApplicationDbContext context, IMapper mapper, INotificationService notificationService, ExamEvaluationService evaluationService)
    {
      _context = context;
      _mapper = mapper;
      _notificationService = notificationService;
      _evaluationService = evaluationService;
    }

    // التحقق من تسجيل دخول المرشح
    private bool IsCandidateLoggedIn()
    {
      return HttpContext.Session.GetInt32("CandidateId") != null;
    }

    // الحصول على معرف المرشح الحالي
    private int? GetCurrentCandidateId()
    {
      return HttpContext.Session.GetInt32("CandidateId");
    }

    // GET: CandidateExams
    public async Task<IActionResult> Index()
    {
      if (!IsCandidateLoggedIn())
      {
        TempData["ErrorMessage"] = "يرجى تسجيل الدخول أولاً.";
        return RedirectToAction("Login", "Auth");
      }

      var candidateId = GetCurrentCandidateId().Value;

      var candidate = await _context.Candidates
          .Include(c => c.Job)
          .FirstOrDefaultAsync(c => c.Id == candidateId);

      if (candidate == null)
      {
        HttpContext.Session.Clear();
        TempData["ErrorMessage"] = "لم يتم العثور على بيانات المرشح.";
        return RedirectToAction("Login", "Auth");
      }

      var candidateViewModel = _mapper.Map<CandidateViewModel>(candidate);

      // الحصول على اختبارات المرشح
      var candidateExams = await _context.CandidateExams
          .Include(ce => ce.Exam)
          .ThenInclude(e => e.Job)
          .Where(ce => ce.CandidateId == candidateId)
          .OrderByDescending(ce => ce.StartTime)
          .ToListAsync();

      //validate if the candidate has an exam that is not completed

      var candidateExamViewModels = _mapper.Map<List<CandidateExamViewModel>>(candidateExams);

      // الحصول على الاختبارات المتاحة لوظيفة المرشح
      var exams = await _context.Exams
          .Include(e => e.Job)
          .Where(e => e.JobId == candidate.JobId && e.Status == ExamStatus.Published)
          .ToListAsync();

      var examViewModels = _mapper.Map<List<ExamForCandidateViewModel>>(exams);

      ViewData["Candidate"] = candidateViewModel;
      ViewData["CandidateExams"] = candidateExamViewModels;

      return View(examViewModels);
    }


    public async Task<IActionResult> ByCandidateId(int id)
    {
      var candidateExams = await _context.CandidateExams
          .Include(ce => ce.Exam)
          .ThenInclude(e => e.Job)
          .Where(ce => ce.CandidateId == id)
          .OrderByDescending(ce => ce.StartTime)
          .ToListAsync();
      var candidateExamViewModels = _mapper.Map<List<CandidateExamViewModel>>(candidateExams);
      ViewData["CandidateExams"] = candidateExamViewModels;
      return View();
    }

    // GET: CandidateExams/Start/5
    [HttpPost]
    public async Task<IActionResult> Start(int id)
    {
      // التحقق من تسجيل دخول المرشح
      var candidateId = GetCurrentCandidateId();
      if (candidateId == null)
      {
        TempData["ErrorMessage"] = "يرجى تسجيل الدخول أولاً.";
        return RedirectToAction("Login", "Auth");
      }

      // الحصول على اختبار المرشح
      var candidateExam = await _context.CandidateExams
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSets)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
        .Include(ce => ce.CandidateAnswers)
        .Include(ce => ce.Candidate)
        .FirstOrDefaultAsync(ce => ce.Id == id && ce.CandidateId == candidateId);

      if (candidateExam == null)
      {
        TempData["ErrorMessage"] = "الاختبار غير موجود.";
        return RedirectToAction("MyExams");
      }

      // التحقق من وجود الأسئلة في الامتحان
      var allQuestions = candidateExam.Exam.ExamQuestionSets
          .SelectMany(eqs => eqs.QuestionSet.Questions)
          .ToList();

      if (candidateExam.Exam == null || allQuestions.Count == 0)
      {
        TempData["ErrorMessage"] = "لا توجد أسئلة في هذا الامتحان.";
        return RedirectToAction("MyExams");
      }

      // تعيين عدد الأسئلة الإجمالي
      int totalQuestions = candidateExam.TotalQuestions > 0 ? candidateExam.TotalQuestions : allQuestions.Count;

      // التحقق مما إذا كان المرشح قد أكمل هذا الاختبار بالفعل
      var completedExam = await _context.CandidateExams
          .FirstOrDefaultAsync(ce => ce.CandidateId == candidateId &&
                                   ce.ExamId == id &&
                                   ce.Status == CandidateExamStatus.Completed.ToString());

      if (completedExam != null)
      {
        TempData["ErrorMessage"] = "لقد أكملت هذا الاختبار بالفعل.";
        return RedirectToAction(nameof(Results), new { id = completedExam.Id });
      }

      // التحقق مما إذا كان المرشح لديه محاولة اختبار غير مكتملة
      var existingAttempt = await _context.CandidateExams
          .FirstOrDefaultAsync(ce => ce.CandidateId == candidateId &&
                                   ce.ExamId == id &&
                                   ce.Status == CandidateExamStatus.InProgress.ToString());

      if (existingAttempt != null)
      {
        // متابعة المحاولة الحالية
        TempData["InfoMessage"] = "أنت تقوم بمتابعة محاولة اختبار سابقة.";
        return RedirectToAction(nameof(Take), new { id = existingAttempt.Id });
      }

      // إنشاء محاولة اختبار جديدة
      var candidateExamNew = new CandidateExam
      {
        CandidateId = candidateId.Value,
        ExamId = id,
        StartTime = DateTime.UtcNow,
        Status = CandidateExamStatus.InProgress.ToString(),
        CreatedAt = DateTime.UtcNow,
        TotalQuestions = totalQuestions,
        CompletedQuestions = 0
      };

      _context.CandidateExams.Add(candidateExamNew);
      await _context.SaveChangesAsync();

      // إنشاء إشعار ببدء الاختبار
      await _notificationService.CreateNotificationAsync(
          candidateId.Value,
          "تم بدء اختبار جديد",
          $"لقد بدأت اختبار {candidateExam.Exam.Name} بنجاح. عدد الأسئلة: {totalQuestions}، مدة الاختبار: {candidateExam.Exam.Duration} دقيقة.",
          "info"
      );

      TempData["SuccessMessage"] = "تم بدء الاختبار بنجاح.";
      return RedirectToAction(nameof(Take), new { id = candidateExamNew.Id });
    }

    // GET: CandidateExams/Take/5
    public async Task<IActionResult> Take(int id)
    {
      // التحقق من تسجيل دخول المرشح
      var candidateId = GetCurrentCandidateId();
      if (candidateId == null)
      {
        TempData["ErrorMessage"] = "يرجى تسجيل الدخول أولاً.";
        return RedirectToAction("Login", "Auth");
      }

      // الحصول على اختبار المرشح
      var candidateExam = await _context.CandidateExams
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSets)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
        .Include(ce => ce.CandidateAnswers)
        .Include(ce => ce.Candidate)
        .ThenInclude(c => c.Job)
        .Include(ce => ce.Exam)
        .Include(ce => ce.Exam.Job)
        .Include(ce => ce.Exam.ExamQuestionSets)

        .FirstOrDefaultAsync(ce => ce.Id == id && ce.CandidateId == candidateId);

      if (candidateExam == null)
      {
        TempData["ErrorMessage"] = "الاختبار غير موجود.";
        return RedirectToAction("MyExams");
      }

      // التحقق من حالة الاختبار
      if (candidateExam.Status != CandidateExamStatus.InProgress.ToString())
      {
        if (candidateExam.Status == CandidateExamStatus.Completed.ToString())
        {
          return RedirectToAction(nameof(Results), new { id = candidateExam.Id });
        }
      }

      // chack if the exam is expired
      if (candidateExam.Exam.Duration.HasValue && candidateExam.StartTime.HasValue)
      {
        var endTime = candidateExam.StartTime.Value.AddMinutes(candidateExam.Exam.Duration.Value);
        if (DateTime.UtcNow > endTime)
        {
          // update the candidate exam status to expired
          candidateExam.Status = CandidateExamStatus.Expired.ToString();
          _context.Update(candidateExam);
          await _context.SaveChangesAsync();

          // create a notification for the candidate
          await _notificationService.CreateNotificationAsync(
              candidateId.Value,
              "الاختبار منتهي",
              $"لقد انتهى الاختبار {candidateExam.Exam.Name}.",
              "warning"
          );

          TempData["ErrorMessage"] = "لقد انتهى الاختبار.";
          return RedirectToAction(nameof(Results), new { id = candidateExam.Id });
        }
      }
      else
      {
        return RedirectToAction(nameof(Results), new { id = candidateExam.Id });
      }

      // جمع جميع الأسئلة من جميع مجموعات الأسئلة في الامتحان
      var allQuestions = candidateExam.Exam.ExamQuestionSets
          .SelectMany(eqs => eqs.QuestionSet.Questions)
          .ToList();

      // التحقق من وجود الأسئلة في الامتحان
      var totalQuestions = candidateExam.TotalQuestions > 0 ? candidateExam.TotalQuestions : allQuestions.Count;
      var completedQuestions = candidateExam.CompletedQuestions > 0 ? candidateExam.CompletedQuestions : 0;

      // الحصول على إجابات المرشح
      var candidateAnswers = await _context.CandidateAnswers
          .Include(ca => ca.Question)
          .Where(ca => ca.CandidateExamId == id)
          .ToListAsync();

      //var candidateExamViewModel = _mapper.Map<CandidateExamViewModel>(candidateExam);
      var candidateExamViewModel = new CandidateExamViewModel()
      {
        Id = candidateExam.Id,
        ExamId = candidateExam.ExamId,
        JobTitle = candidateExam.Exam.Job.Title,
        CandidateName = candidateExam.Candidate.Name,
        CandidateId = candidateExam.CandidateId,
        StartTime = candidateExam.StartTime,
        EndTime = candidateExam.EndTime,
        Duration = candidateExam.Exam.Duration.Value,
        TotalQuestions = totalQuestions,
        CompletedQuestions = completedQuestions,
        Score = candidateExam.Score,
        Status = candidateExam.Status,
        FlaggedQuestions = candidateAnswers.Where(ca => ca.IsFlagged).Select(ca => ca.QuestionId).ToList()
      };

      // تحميل الأسئلة في نموذج العرض
      candidateExamViewModel.Questions = _mapper.Map<List<CandidateQuestionViewModel>>(allQuestions);


      // إعداد بيانات إضافية للعرض
      var remainingTime = CalculateRemainingTime(candidateExam);
      candidateExamViewModel.RemainingTime = remainingTime;

      ViewData["CandidateAnswers"] = candidateAnswers;
      ViewData["ProgressPercentage"] = CalculateProgressPercentage(candidateExam, candidateAnswers);

      return View(candidateExamViewModel);
    }

    // حساب النسبة المئوية للتقدم في الاختبار
    private int CalculateProgressPercentage(CandidateExam candidateExam, List<CandidateAnswer> candidateAnswers)
    {
      if (candidateExam.Exam == null || candidateExam.Exam.ExamQuestionSets == null || !candidateExam.Exam.ExamQuestionSets.Any() ||
          !candidateExam.Exam.ExamQuestionSets.Any(eqs => eqs.QuestionSet.Questions != null && eqs.QuestionSet.Questions.Any()))
      {
        return 0;
      }

      int totalQuestions = candidateExam.TotalQuestions > 0 ? candidateExam.TotalQuestions :
          candidateExam.Exam.ExamQuestionSets.SelectMany(eqs => eqs.QuestionSet.Questions).Count();
      int answeredQuestions = candidateAnswers.Select(ca => ca.QuestionId).Distinct().Count();

      return (int)Math.Round((double)answeredQuestions / totalQuestions * 100);
    }

    // حساب الوقت المتبقي للاختبار
    private TimeSpan? CalculateRemainingTime(CandidateExam candidateExam)
    {
      if (!candidateExam.StartTime.HasValue || candidateExam.Exam == null || !candidateExam.Exam.Duration.HasValue)
      {
        return null;
      }

      DateTime endTime = candidateExam.StartTime.Value.AddMinutes(candidateExam.Exam.Duration.Value);
      TimeSpan remaining = endTime - DateTime.UtcNow;

      return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
    }

    // POST: CandidateExams/SaveAnswer
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAnswer(SaveAnswerDTO model)
    {
      // التحقق من تسجيل دخول المرشح
      var candidateId = GetCurrentCandidateId();
      if (candidateId == null)
      {
        return Unauthorized(new { success = false, message = "يرجى تسجيل الدخول أولاً." });
      }


      // الحصول على اختبار المرشح
      var candidateExam = await _context.CandidateExams
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSets)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
        .FirstOrDefaultAsync(ce => ce.Id == model.CandidateExamId && ce.CandidateId == candidateId);

      if (candidateExam == null || candidateExam.Status == CandidateExamStatus.Completed.ToString())
      {
        return BadRequest(new { success = false, message = "الاختبار غير متاح." });
      }

      // الحصول على السؤال
      var question = await _context.Questions
          .Include(q => q.Options)
          .Include(q => q.MatchingPairs)
          .Include(q => q.OrderingItems)
          .FirstOrDefaultAsync(q => q.Id == model.QuestionId);

      if (question == null)
      {
        return BadRequest(new { success = false, message = "السؤال غير موجود." });
      }

      // التحقق مما إذا كانت الإجابة موجودة بالفعل
      var existingAnswer = await _context.CandidateAnswers
          .FirstOrDefaultAsync(ca => ca.CandidateExamId == model.CandidateExamId && ca.QuestionId == model.QuestionId);

      bool? isCorrect = null;

      // التحقق مما إذا كانت الإجابة صحيحة
      switch (question.QuestionType)
      {
        case "MCQ":
          var selectedOption = question.Options.FirstOrDefault(o => o.Id.ToString() == model.AnswerText);
          isCorrect = selectedOption?.IsCorrect;
          break;

        case "TF":
          if (bool.TryParse(model.AnswerText, out bool boolAnswer))
          {
            isCorrect = boolAnswer == question.TrueFalseAnswer;
          }
          break;

        case "ShortAnswer":
        case "FillInTheBlank":
          isCorrect = string.Equals(question.Answer, model.AnswerText, StringComparison.OrdinalIgnoreCase);
          break;

        case "MultiSelect":
          try
          {
            var selectedOptions = model.SelectedOptionsIds;
            var correctOptions = question.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToList();
            isCorrect = selectedOptions != null && selectedOptions.OrderBy(x => x).SequenceEqual(correctOptions.OrderBy(x => x));
          }
          catch
          {
            isCorrect = false;
          }
          break;

        case "Matching":
          try
          {
            var pairs = model.MatchingPairsIds;
            var correctPairs = question.MatchingPairs.Select(p => p.Id).ToList();
            isCorrect = pairs != null && pairs.OrderBy(x => x).SequenceEqual(correctPairs.OrderBy(x => x));
          }
          catch
          {
            isCorrect = false;
          }
          break;

        case "Ordering":
          try
          {
            var orderItems = model.OrderingItemsIds;
            var correctOrderItems = question.OrderingItems.Where(ss => ss.DisplayOrder == 0).OrderBy(o => o.CorrectOrder).Select(o => o.Id).ToList();
            isCorrect = orderItems != null && orderItems.OrderBy(x => x).SequenceEqual(correctOrderItems.OrderBy(x => x));
          }
          catch
          {
            isCorrect = false;
          }
          break;
      }

      if (existingAnswer != null)
      {
        // تحديث الإجابة الموجودة
        existingAnswer.AnswerText = model.AnswerText;
        existingAnswer.IsCorrect = isCorrect;
        existingAnswer.UpdatedAt = DateTime.UtcNow;
        _context.Update(existingAnswer);
      }
      else
      {
        // إنشاء إجابة جديدة
        var candidateAnswer = new CandidateAnswer
        {
          CandidateExamId = model.CandidateExamId,
          QuestionId = model.QuestionId,
          AnswerText = model.AnswerText,
          IsCorrect = isCorrect,
          CreatedAt = DateTime.UtcNow,
        };

        _context.CandidateAnswers.Add(candidateAnswer);

        // تحديث عدد الأسئلة المجابة
        candidateExam.CompletedQuestions = await _context.CandidateAnswers
            .Where(ca => ca.CandidateExamId == model.CandidateExamId)
            .Select(ca => ca.QuestionId)
            .Distinct()
            .CountAsync() + 1; // نضيف 1 للإجابة الحالية

        _context.Update(candidateExam);
      }

      await _context.SaveChangesAsync();

      return Ok(new { success = true, message = "تم حفظ الإجابة بنجاح." });
    }

    // POST: CandidateExams/Submit
    [HttpPost]
    public async Task<IActionResult> Submit(int id)
    {
      // التحقق من تسجيل دخول المرشح
      var candidateId = GetCurrentCandidateId();
      if (candidateId == null)
      {
        TempData["ErrorMessage"] = "يرجى تسجيل الدخول أولاً.";
        return RedirectToAction("Login", "Auth");
      }

      // الحصول على اختبار المرشح
      var candidateExam = await _context.CandidateExams
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSets)
          .Include(ce => ce.CandidateAnswers)
          .Include(ce => ce.Candidate)
          .FirstOrDefaultAsync(ce => ce.Id == id && ce.CandidateId == candidateId);

      if (candidateExam == null)
      {
        return NotFound();
      }

      if (candidateExam.Status == CandidateExamStatus.Completed.ToString())
      {
        return RedirectToAction(nameof(Results), new { id = candidateExam.Id });
      }

      // تحديث حالة الاختبار أولاً
      candidateExam.EndTime = DateTime.UtcNow;
      candidateExam.Status = CandidateExamStatus.Completed.ToString();
      candidateExam.UpdatedAt = DateTime.UtcNow;

      _context.Update(candidateExam);
      await _context.SaveChangesAsync();

      // حساب النتيجة باستخدام نظام التقييم المحسن
      var evaluationResult = await _evaluationService.CalculateEnhancedScoreAsync(candidateExam.Id);

      // حفظ ترتيب المرشحين للاختبار
      await _evaluationService.RankCandidatesAsync(candidateExam.ExamId);

      // إنشاء إشعار بنتيجة الاختبار المحسنة
      var passPercentage = candidateExam.Exam.PassPercentage ?? 60;
      var hasPassed = evaluationResult.ScorePercentage >= passPercentage;

      await _notificationService.CreateNotificationAsync(
          candidateId.Value,
          "نتيجة الاختبار",
          $"لقد أنهيت اختبار {candidateExam.Exam.Name} بنجاح. النقاط: {evaluationResult.TotalPointsEarned}/{evaluationResult.MaxPossiblePoints} - النسبة: {evaluationResult.ScorePercentage:F1}%. " + (hasPassed ? "مبروك! لقد اجتزت الاختبار." : "للأسف، لم تحقق درجة النجاح."),
          hasPassed ? "success" : "warning"
      );

      TempData["SuccessMessage"] = "تم تسليم الاختبار بنجاح";
      return RedirectToAction(nameof(Results), new { id = candidateExam.Id });
    }

    // GET: CandidateExams/Results/5
    public async Task<IActionResult> Results(int id)
    {
      // التحقق من تسجيل دخول المرشح
      var candidateId = GetCurrentCandidateId();
      if (candidateId == null)
      {
        TempData["ErrorMessage"] = "يرجى تسجيل الدخول أولاً.";
        return RedirectToAction("Login", "Auth");
      }

      // الحصول على تفاصيل اختبار المرشح
      var candidateExam = await _context.CandidateExams
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSets)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
                          .ThenInclude(q => q.Options)
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSets)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
                          .ThenInclude(q => q.MatchingPairs)
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSets)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
                          .ThenInclude(q => q.OrderingItems)
          .Include(ce => ce.CandidateAnswers)
              .ThenInclude(ca => ca.Question)
          .Include(ce => ce.Candidate)
          .FirstOrDefaultAsync(ce => ce.Id == id && ce.CandidateId == candidateId);

      if (candidateExam == null)
      {
        return NotFound();
      }

      var candidateExamResult = _mapper.Map<CandidateExamResultViewModel>(candidateExam);

      // تعيين إعداد عرض النتائج من بيانات الاختبار
      candidateExamResult.ShowResultsImmediately = candidateExam.Exam.ShowResultsImmediately;

      // جمع جميع الأسئلة من جميع مجموعات الأسئلة في الامتحان
      var allQuestions = candidateExam.Exam.ExamQuestionSets
          .SelectMany(eqs => eqs.QuestionSet.Questions)
          .ToList();

      // ترتيب الإجابات حسب أرقام الأسئلة
      candidateExamResult.Answers = _mapper.Map<List<CandidateAnswerViewModel>>(
          candidateExam.CandidateAnswers.OrderBy(ca => ca.Question.DisplayOrder));

      ViewData["CandidateAnswers"] = candidateExamResult.Answers;
      ViewData["PassPercentage"] = candidateExam.Exam?.PassPercentage ?? 60;
      ViewData["HasPassed"] = candidateExam.Score >= (candidateExam.Exam?.PassPercentage ?? 60);

      return View(candidateExamResult);
    }


    // POST: CandidateExams/ReplaceQuestion
    [HttpPost]
    public async Task<IActionResult> ReplaceQuestion(int candidateExamId, int questionId)
    {
      // التحقق من تسجيل دخول المرشح
      var candidateId = GetCurrentCandidateId();
      if (candidateId == null)
      {
        return Unauthorized(new { success = false, message = "يرجى تسجيل الدخول أولاً." });
      }

      // الحصول على اختبار المرشح
      var candidateExam = await _context.CandidateExams
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSets)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
        .Include(ce => ce.CandidateAnswers)
        .FirstOrDefaultAsync(ce => ce.Id == candidateExamId && ce.CandidateId == candidateId);

      if (candidateExam == null || candidateExam.Status == CandidateExamStatus.Completed.ToString())
      {
        return BadRequest(new { success = false, message = "الاختبار غير متاح." });
      }

      // التحقق مما إذا كان قد تم استبدال سؤال بالفعل
      if (candidateExam.QuestionReplaced)
      {
        return BadRequest(new { success = false, message = "لا يمكنك استبدال أكثر من سؤال واحد." });
      }

      // جمع جميع الأسئلة من جميع مجموعات الأسئلة في الامتحان
      var allQuestions = candidateExam.Exam.ExamQuestionSets
          .SelectMany(eqs => eqs.QuestionSet.Questions)
          .ToList();

      // إذا كان هناك فقط سؤال واحد متبقي، فلا يمكن استبدال السؤال
      if (allQuestions.Count <= 1)
      {
        return BadRequest(new { success = false, message = "لا يمكنك استبدال السؤال إذا كان هناك سؤال واحد فقط." });
      }

      // الحصول على السؤال المراد استبداله
      var questionToReplace = await _context.Questions
          .Include(q => q.QuestionSet)
              .ThenInclude(qs => qs.ExamQuestionSets)
          .FirstOrDefaultAsync(q => q.Id == questionId);

      if (questionToReplace == null || !questionToReplace.QuestionSet.ExamQuestionSets.Any(eqs => eqs.ExamId == candidateExam.ExamId))
      {
        return Json(new { success = false, message = "السؤال غير موجود" });
      }

      // البحث عن سؤال بديل من نفس النوع والصعوبة
      // أولاً نحصل على مجموعات الأسئلة المرتبطة بهذا الامتحان
      var questionSetIds = await _context.ExamQuestionSets
          .Where(eqs => eqs.ExamId == candidateExam.ExamId)
          .Select(eqs => eqs.QuestionSetId)
          .ToListAsync();

      var replacementQuestion = await _context.Questions
          .Where(q => questionSetIds.Contains(q.QuestionSetId) &&
                     q.QuestionType == questionToReplace.QuestionType &&
                     q.Id != questionId &&
                     !_context.CandidateAnswers.Any(ca => ca.CandidateExamId == candidateExamId && ca.QuestionId == q.Id))
          .OrderBy(q => Guid.NewGuid()) // اختيار عشوائي
          .FirstOrDefaultAsync();

      if (replacementQuestion == null)
      {
        return Json(new { success = false, message = "لا يوجد سؤال بديل متاح" });
      }

      // حذف الإجابة الموجودة إن وجدت
      var existingAnswer = await _context.CandidateAnswers
          .FirstOrDefaultAsync(ca => ca.CandidateExamId == candidateExamId && ca.QuestionId == questionId);

      if (existingAnswer != null)
      {
        _context.CandidateAnswers.Remove(existingAnswer);
      }

      // تحديث حالة استبدال السؤال في الاختبار
      candidateExam.QuestionReplaced = true;
      candidateExam.UpdatedAt = DateTime.UtcNow;
      _context.Update(candidateExam);

      await _context.SaveChangesAsync();

      // إضافة إشعار باستبدال السؤال
      await _notificationService.CreateNotificationAsync(
          candidateId.Value,
          "تم استبدال سؤال",
          $"تم استبدال السؤال بنجاح في الاختبار {candidateExam.Exam.Name}.",
          "info"
      );

      return Json(new { success = true, replacementQuestionId = replacementQuestion.Id });
    }

    // POST: CandidateExams/FlagQuestion
    [HttpPost]
    public async Task<IActionResult> FlagQuestion(QuestionFlagDTO model)
    {
      // التحقق من تسجيل دخول المرشح
      var candidateId = GetCurrentCandidateId();
      if (candidateId == null)
      {
        return Unauthorized(new { success = false, message = "يرجى تسجيل الدخول أولاً." });
      }

      // الحصول على اختبار المرشح
      var candidateExam = await _context.CandidateExams
          .FirstOrDefaultAsync(ce => ce.Id == model.CandidateExamId && ce.CandidateId == candidateId);

      if (candidateExam == null)
      {
        return NotFound(new { success = false, message = "لم يتم العثور على الاختبار." });
      }

      // الحصول على السؤال
      var question = await _context.Questions
          .FirstOrDefaultAsync(q => q.Id == model.QuestionId);

      if (question == null)
      {
        return NotFound(new { success = false, message = "لم يتم العثور على السؤال." });
      }

      // البحث عن الإجابة الموجودة لإضافة علامة عليها
      var candidateAnswer = await _context.CandidateAnswers
          .FirstOrDefaultAsync(ca => ca.CandidateExamId == model.CandidateExamId && ca.QuestionId == model.QuestionId);

      // إذا لم توجد إجابة، قم بإنشاء واحدة مؤقتة فقط لتخزين حالة العلامة
      if (candidateAnswer == null)
      {
        candidateAnswer = new CandidateAnswer
        {
          CandidateExamId = model.CandidateExamId,
          QuestionId = model.QuestionId,
          IsFlagged = model.IsFlagged,
          CreatedAt = DateTime.UtcNow
        };
        _context.CandidateAnswers.Add(candidateAnswer);
      }
      else
      {
        // تحديث حالة العلامة في الإجابة الموجودة
        candidateAnswer.IsFlagged = model.IsFlagged;
        candidateAnswer.UpdatedAt = DateTime.UtcNow;
        _context.Update(candidateAnswer);
      }

      await _context.SaveChangesAsync();

      return Ok(new { success = true, message = model.IsFlagged ? "تم تعليم السؤال للمراجعة لاحقاً." : "تم إلغاء تعليم السؤال." });
    }

    // البحث عن السؤال التالي غير المُجاب عليه (إذا كان هناك أي)
    private async Task<Question> GetNextUnansweredQuestion(int candidateExamId)
    {
      var candidateExam = await _context.CandidateExams
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSets)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
      .Include(ce => ce.CandidateAnswers)
      .FirstOrDefaultAsync(ce => ce.Id == candidateExamId);

      if (candidateExam == null)
      {
        return null;
      }

      // جمع جميع الأسئلة من جميع مجموعات الأسئلة في الامتحان
      var allQuestions = candidateExam.Exam.ExamQuestionSets
          .SelectMany(eqs => eqs.QuestionSet.Questions)
          .ToList();

      // الحصول على معرّفات الأسئلة التي تمت الإجابة عليها بالفعل
      var answeredQuestionIds = candidateExam.CandidateAnswers
          .Select(ca => ca.QuestionId)
          .ToHashSet();

      // تصفية الأسئلة غير المُجاب عليها
      var unansweredQuestions = allQuestions
          .Where(q => !answeredQuestionIds.Contains(q.Id))
          .ToList();

      // إذا كانت جميع الأسئلة قد تمت الإجابة عليها، فارجع null
      if (unansweredQuestions.Count == 0)
      {
        return null;
      }

      // اختيار السؤال التالي
      return unansweredQuestions.FirstOrDefault();
    }

    // الحصول على السؤال الحالي استنادًا إلى رقم المؤشر
    [HttpGet]
    public async Task<IActionResult> GetQuestion(int candidateExamId, int questionIndex)
    {
      // التحقق من تسجيل دخول المرشح
      var candidateId = GetCurrentCandidateId();
      if (candidateId == null)
      {
        return Unauthorized(new { success = false, message = "يرجى تسجيل الدخول أولاً." });
      }

      // الحصول على اختبار المرشح
      var candidateExam = await _context.CandidateExams
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSets)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
                          .ThenInclude(q => q.Options)
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSets)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
                          .ThenInclude(q => q.MatchingPairs)
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSets)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
                          .ThenInclude(q => q.OrderingItems)
          .Include(ce => ce.CandidateAnswers)
          .FirstOrDefaultAsync(ce => ce.Id == candidateExamId && ce.CandidateId == candidateId);

      if (candidateExam == null)
      {
        return NotFound(new { success = false, message = "الاختبار غير موجود." });
      }

      // جمع جميع الأسئلة من جميع مجموعات الأسئلة في الامتحان
      var allQuestions = candidateExam.Exam.ExamQuestionSets
          .SelectMany(eqs => eqs.QuestionSet.Questions)
          .ToList();

      // التحقق من صحة المؤشر
      if (questionIndex < 0 || questionIndex >= allQuestions.Count)
      {
        return BadRequest(new { success = false, message = "رقم السؤال غير صالح." });
      }

      // الحصول على السؤال
      var question = allQuestions[questionIndex];

      // الحصول على إجابة المرشح (إن وجدت)
      var candidateAnswer = await _context.CandidateAnswers
          .FirstOrDefaultAsync(ca => ca.CandidateExamId == candidateExamId && ca.QuestionId == question.Id);

      var questionViewModel = _mapper.Map<CandidateQuestionViewModel>(question);
      questionViewModel.IsAnswered = candidateAnswer != null && !string.IsNullOrEmpty(candidateAnswer.AnswerText);
      questionViewModel.IsFlagged = candidateAnswer != null && candidateAnswer.IsFlagged;

      if (question.QuestionType == nameof(QuestionTypeEnum.Matching))
      {
        questionViewModel.MatchingPairs = _mapper.Map<List<MatchingPairViewModel>>(question.MatchingPairs);
      }

      if (question.QuestionType == nameof(QuestionTypeEnum.Ordering))
      {
        questionViewModel.OrderingItems = _mapper.Map<List<OrderingItemViewModel>>(question.OrderingItems);
      }

      if (question.QuestionType == nameof(QuestionTypeEnum.TF))
      {
        questionViewModel.TrueFalseAnswer = candidateAnswer != null && candidateAnswer.AnswerText == "True";
      }

      return Ok(new
      {
        success = true,
        question = questionViewModel,
        currentIndex = questionIndex,
        totalQuestions = allQuestions.Count,
        answer = candidateAnswer?.AnswerText
      });
    }

    // POST: CandidateExams/Create
    [HttpPost]
    public async Task<IActionResult> Create(int examId)
    {
      // التحقق من تسجيل دخول المرشح
      var candidateId = GetCurrentCandidateId();
      if (candidateId == null)
      {
        TempData["ErrorMessage"] = "يرجى تسجيل الدخول أولاً.";
        return RedirectToAction("Login", "Auth");
      }

      // التحقق من وجود الامتحان
      var exam = await _context.Exams
          .Include(e => e.ExamQuestionSets)
              .ThenInclude(eqs => eqs.QuestionSet)
                  .ThenInclude(qs => qs.Questions)
          .FirstOrDefaultAsync(e => e.Id == examId);

      if (exam == null)
      {
        TempData["ErrorMessage"] = "الامتحان غير موجود.";
        return RedirectToAction("Index", "Home");
      }

      // إضافة كود لإنشاء محاولة اختبار جديدة
      return RedirectToAction("Start", new { id = examId });
    }

    // إحضار سؤال لامتحان مرشح
    private async Task<List<Question>> GetQuestionsForCandidateExam(int candidateExamId)
    {
      var candidateExam = await _context.CandidateExams
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSets)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
          .FirstOrDefaultAsync(ce => ce.Id == candidateExamId);

      if (candidateExam == null)
      {
        return new List<Question>();
      }

      // جمع جميع الأسئلة من جميع مجموعات الأسئلة في الامتحان
      var allQuestions = candidateExam.Exam.ExamQuestionSets
          .SelectMany(eqs => eqs.QuestionSet.Questions)
          .ToList();

      return allQuestions;
    }

    // POST: CandidateExams/Complete
    [HttpPost]
    public async Task<IActionResult> Complete(int candidateExamId)
    {
      // التحقق من تسجيل دخول المرشح
      var candidateId = GetCurrentCandidateId();
      if (candidateId == null)
      {
        return Unauthorized(new { success = false, message = "يرجى تسجيل الدخول أولاً." });
      }

      // الحصول على اختبار المرشح
      var candidateExam = await _context.CandidateExams
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSets)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
          .Include(ce => ce.CandidateAnswers)
              .ThenInclude(ca => ca.Question)
          .FirstOrDefaultAsync(ce => ce.Id == candidateExamId && ce.CandidateId == candidateId);

      if (candidateExam == null)
      {
        return BadRequest(new { success = false, message = "الاختبار غير موجود." });
      }

      if (candidateExam.Status == CandidateExamStatus.Completed.ToString())
      {
        return BadRequest(new { success = false, message = "الاختبار مكتمل بالفعل." });
      }

      // جمع جميع الأسئلة من جميع مجموعات الأسئلة في الامتحان
      var allQuestions = candidateExam.Exam.ExamQuestionSets
          .SelectMany(eqs => eqs.QuestionSet.Questions)
          .ToList();

      // حساب عدد الإجابات الصحيحة
      int correctAnswers = candidateExam.CandidateAnswers.Count(ca => ca.IsCorrect.HasValue && ca.IsCorrect.Value);

      // حساب النتيجة النهائية
      decimal totalQuestions = allQuestions.Count;
      decimal score = totalQuestions > 0 ? (correctAnswers / totalQuestions) * 100 : 0;

      // تحديث حالة الاختبار ونتيجته
      candidateExam.Score = score;
      candidateExam.EndTime = DateTime.UtcNow;
      candidateExam.Status = CandidateExamStatus.Completed.ToString();
      candidateExam.UpdatedAt = DateTime.UtcNow;

      await _context.SaveChangesAsync();

      return Ok(new
      {
        success = true,
        score = score,
        redirectUrl = Url.Action("Results", new { id = candidateExamId })
      });
    }
  }
}
