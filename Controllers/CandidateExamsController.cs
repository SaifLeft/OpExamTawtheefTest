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

    public CandidateExamsController(ApplicationDbContext context, IMapper mapper, INotificationService notificationService)
    {
      _context = context;
      _mapper = mapper;
      _notificationService = notificationService;
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
      var candidateExams = await _context.Assignments
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
      var candidateExams = await _context.Assignments
          .Include(ce => ce.Exam)
          .ThenInclude(e => e.Job)
          .Where(ce => ce.CandidateId == id)
          .OrderByDescending(ce => ce.StartTime)
          .ToListAsync();
      var candidateExamViewModels = _mapper.Map<List<CandidateExamViewModel>>(candidateExams);
      ViewData["CandidateExams"] = candidateExamViewModels;
      return View();
    }

    // GET: CandidateExams/Start/5 - عرض صفحة التعليمات قبل بدء الامتحان
    [HttpGet]
    public async Task<IActionResult> Start(int id)
    {
      // التحقق من تسجيل دخول المرشح
      var candidateId = GetCurrentCandidateId();
      if (candidateId == null)
      {
        TempData["ErrorMessage"] = "يرجى تسجيل الدخول أولاً.";
        return RedirectToAction("Login", "Auth");
      }

      // الحصول على الامتحان
      var exam = await _context.Exams
          .Include(e => e.Job)
          .Include(e => e.ExamQuestionSetManppings)
              .ThenInclude(eqs => eqs.QuestionSet)
                  .ThenInclude(qs => qs.Questions)
          .FirstOrDefaultAsync(e => e.Id == id);

      if (exam == null)
      {
        TempData["ErrorMessage"] = "الامتحان غير موجود.";
        return RedirectToAction("Index");
      }

      // الحصول على بيانات المرشح
      var candidate = await _context.Candidates
          .Include(c => c.Job)
          .FirstOrDefaultAsync(c => c.Id == candidateId.Value);

      if (candidate == null)
      {
        HttpContext.Session.Clear();
        TempData["ErrorMessage"] = "لم يتم العثور على بيانات المرشح.";
        return RedirectToAction("Login", "Auth");
      }

      // التحقق من أن الامتحان مخصص لوظيفة المرشح
      if (exam.JobId != candidate.JobId)
      {
        TempData["ErrorMessage"] = "هذا الامتحان غير متاح لوظيفتك.";
        return RedirectToAction("Index");
      }

      // التحقق مما إذا كان المرشح قد أكمل هذا الاختبار بالفعل
      var completedExam = await _context.Assignments
          .FirstOrDefaultAsync(ce => ce.CandidateId == candidateId &&
                                   ce.ExamId == id &&
                                   ce.Status == CandidateExamStatus.Completed.ToString());

      if (completedExam != null)
      {
        TempData["ErrorMessage"] = "لقد أكملت هذا الاختبار بالفعل.";
        return RedirectToAction(nameof(Results), new { id = completedExam.Id });
      }

      // التحقق من وجود محاولة قيد التقدم
      var existingAttempt = await _context.Assignments
          .FirstOrDefaultAsync(ce => ce.CandidateId == candidateId &&
                                   ce.ExamId == id &&
                                   ce.Status == CandidateExamStatus.InProgress.ToString());

      // حساب عدد الأسئلة الإجمالي
      var allQuestions = exam.ExamQuestionSetManppings
          .SelectMany(eqs => eqs.QuestionSet.Questions)
          .ToList();

      var totalQuestions = exam.TotalQuestionsPerCandidate > 0 ? exam.TotalQuestionsPerCandidate : allQuestions.Count;

      // إنشاء نموذج العرض
      var viewModel = new ExamInstructionsViewModel
      {
        ExamId = exam.Id,
        ExamName = exam.Name,
        ExamDescription = exam.Description ?? "لا يوجد وصف للامتحان",
        JobName = exam.Job.Title,
        Duration = exam.Duration,
        TotalQuestions = totalQuestions,
        PassPercentage = exam.PassPercentage,
        CandidateName = candidate.Name,
        HasExistingAttempt = existingAttempt != null,
        ExistingAttemptId = existingAttempt?.Id,
        ExamRules = GetExamRules(),
        TechnicalInstructions = GetTechnicalInstructions()
      };

      return View(viewModel);
    }

    // POST: CandidateExams/StartExam/5 - بدء الامتحان فعلياً
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartExam(int id)
    {
      // التحقق من تسجيل دخول المرشح
      var candidateId = GetCurrentCandidateId();
      if (candidateId == null)
      {
        TempData["ErrorMessage"] = "يرجى تسجيل الدخول أولاً.";
        return RedirectToAction("Login", "Auth");
      }

      // الحصول على الامتحان
      var exam = await _context.Exams
          .Include(e => e.Job)
          .Include(e => e.ExamQuestionSetManppings)
              .ThenInclude(eqs => eqs.QuestionSet)
                  .ThenInclude(qs => qs.Questions)
          .FirstOrDefaultAsync(e => e.Id == id);

      if (exam == null)
      {
        TempData["ErrorMessage"] = "الامتحان غير موجود.";
        return RedirectToAction("Index");
      }

      // التحقق من وجود الأسئلة في الامتحان
      var allQuestions = exam.ExamQuestionSetManppings
          .SelectMany(eqs => eqs.QuestionSet.Questions)
          .ToList();

      if (allQuestions.Count == 0)
      {
        TempData["ErrorMessage"] = "لا توجد أسئلة في هذا الامتحان.";
        return RedirectToAction("Index");
      }

      // تعيين عدد الأسئلة الإجمالي
      int totalQuestions = exam.TotalQuestionsPerCandidate > 0 ? exam.TotalQuestionsPerCandidate : allQuestions.Count;

      // التحقق مما إذا كان المرشح قد أكمل هذا الاختبار بالفعل
      var completedExam = await _context.Assignments
          .FirstOrDefaultAsync(ce => ce.CandidateId == candidateId &&
                                   ce.ExamId == id &&
                                   ce.Status == CandidateExamStatus.Completed.ToString());

      if (completedExam != null)
      {
        TempData["ErrorMessage"] = "لقد أكملت هذا الاختبار بالفعل.";
        return RedirectToAction(nameof(Results), new { id = completedExam.Id });
      }

      // التحقق مما إذا كان المرشح لديه محاولة اختبار غير مكتملة
      var existingAttempt = await _context.Assignments
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
        StartTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
        Status = CandidateExamStatus.InProgress.ToString(),
        CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
        TotalQuestions = totalQuestions,
        CompletedQuestions = 0
      };

      _context.CandidateExams.Add(candidateExamNew);
      await _context.SaveChangesAsync();

      // إنشاء إشعار ببدء الاختبار
      await _notificationService.CreateNotificationAsync(
          candidateId.Value,
          "تم بدء اختبار جديد",
          $"لقد بدأت اختبار {exam.Name} بنجاح. عدد الأسئلة: {totalQuestions}، مدة الاختبار: {exam.Duration} دقيقة.",
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
      var candidateExam = await _context.Assignments
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSetManppings)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
        .Include(ce => ce.CandidateAnswers)
        .Include(ce => ce.Candidate)
        .ThenInclude(c => c.Job)
        .Include(ce => ce.Exam)
        .Include(ce => ce.Exam.Job)
        .Include(ce => ce.Exam.ExamQuestionSetManppings)

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
        else
        {
          return RedirectToAction(nameof(Start), new { id = candidateExam.Id });
        }
      }

      // جمع جميع الأسئلة من جميع مجموعات الأسئلة في الامتحان
      var allQuestions = candidateExam.Exam.ExamQuestionSetManppings
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
        StartTime = candidateExam.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
        EndTime = candidateExam.EndTime.ToString("yyyy-MM-dd HH:mm:ss"),
        Duration = candidateExam.Exam.Duration ?? 30,
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
    private long CalculateProgressPercentage(Assignment candidateExam, List<CandidateAnswer> candidateAnswers)
    {
      if (candidateExam.Exam == null || candidateExam.Exam.ExamQuestionSetManppings == null || !candidateExam.Exam.ExamQuestionSetManppings.Any() ||
          !candidateExam.Exam.ExamQuestionSetManppings.Any(eqs => eqs.QuestionSet.Questions != null && eqs.QuestionSet.Questions.Any()))
      {
        return 0;
      }

      long totalQuestions = candidateExam.TotalQuestions > 0 ? candidateExam.TotalQuestions :
          candidateExam.Exam.ExamQuestionSetManppings.SelectMany(eqs => eqs.QuestionSet.Questions).Count();
      long answeredQuestions = candidateAnswers.Select(ca => ca.QuestionId).Distinct().Count();

      return (long)Math.Round((double)answeredQuestions / totalQuestions * 100);
    }

    // حساب الوقت المتبقي للاختبار
    private TimeSpan? CalculateRemainingTime(Assignment candidateExam)
    {
      if (!candidateExam.StartTime.HasValue || candidateExam.Exam == null)
      {
        return null;
      }

      DateTime endTime = candidateExam.StartTime.AddMinutes(candidateExam.Exam.Duration);
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
      var candidateExam = await _context.Assignments
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSetManppings)
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
          // أسئلة الإجابة القصيرة - مقارنة نصية
          isCorrect = string.Equals(question.Answer, model.AnswerText, StringComparison.OrdinalIgnoreCase);
          break;

        case "FillInTheBlank":
          // أسئلة ملء الفراغات - قد تحتوي على خيارات أو إجابة نصية
          if (question.Options != null && question.Options.Any() && !string.IsNullOrEmpty(model.AnswerText))
          {
            // إذا كان هناك خيارات، تحقق من الخيار المحدد
            var fillBlankSelectedOption = question.Options.FirstOrDefault(o => o.Id.ToString() == model.AnswerText);
            isCorrect = fillBlankSelectedOption?.IsCorrect ?? false;
          }
          else
          {
            // إذا لم تكن هناك خيارات، قارن النص مباشرة
            isCorrect = string.Equals(question.Answer, model.AnswerText, StringComparison.OrdinalIgnoreCase);
          }
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
            if (orderItems != null && orderItems.Count > 0)
            {
              // الحصول على الترتيب الصحيح للعناصر
              var correctOrder = question.OrderingItems
                  .OrderBy(o => o.CorrectOrder)
                  .Select(o => o.Id)
                  .ToList();

              // مقارنة ترتيب المرشح مع الترتيب الصحيح
              isCorrect = orderItems.SequenceEqual(correctOrder);
            }
            else
            {
              isCorrect = false;
            }
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
        existingAnswer.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
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
          CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
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
      var candidateExam = await _context.Assignments
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSetManppings)
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

      // حساب الدرجة
      var totalQuestions = candidateExam.TotalQuestions > 0 ? candidateExam.TotalQuestions :
          candidateExam.Exam.ExamQuestionSetManppings.SelectMany(eqs => eqs.QuestionSet.Questions).Count();
      var answeredQuestions = candidateExam.CandidateAnswers.Select(ca => ca.QuestionId).Distinct().Count();
      var correctAnswers = candidateExam.CandidateAnswers.Count(ca => ca.IsCorrect == true);
      var score = totalQuestions > 0 ? (decimal)correctAnswers / totalQuestions * 100 : 0;

      // تحديث اختبار المرشح
      candidateExam.EndTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
      candidateExam.Score = Math.Round(score, 2);
      candidateExam.Status = CandidateExamStatus.Completed.ToString();
      candidateExam.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
      candidateExam.CompletedQuestions = answeredQuestions;
      candidateExam.TotalQuestions = totalQuestions;

      _context.Update(candidateExam);
      await _context.SaveChangesAsync();

      // إنشاء إشعار بنتيجة الاختبار
      var passPercentage = candidateExam.Exam.PassPercentage ?? 60;
      var hasPassed = candidateExam.Score >= passPercentage;

      await _notificationService.CreateNotificationAsync(
          candidateId.Value,
          "نتيجة الاختبار",
          $"لقد أنهيت اختبار {candidateExam.Exam.Name} بنجاح. الدرجة: {candidateExam.Score}%. " + (hasPassed ? "مبروك! لقد اجتزت الاختبار." : "للأسف، لم تحقق درجة النجاح."),
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
      var candidateExam = await _context.Assignments
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSetManppings)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
                          .ThenInclude(q => q.Options)
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSetManppings)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
                          .ThenInclude(q => q.MatchingPairs)
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSetManppings)
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

      // جمع جميع الأسئلة من جميع مجموعات الأسئلة في الامتحان
      var allQuestions = candidateExam.Exam.ExamQuestionSetManppings
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

    // GET: CandidateExams/MyExams
    public async Task<IActionResult> MyExams()
    {
      // التحقق من تسجيل دخول المرشح
      var candidateId = GetCurrentCandidateId();
      if (candidateId == null)
      {
        TempData["ErrorMessage"] = "يرجى تسجيل الدخول أولاً.";
        return RedirectToAction("Login", "Auth");
      }

      var candidateExams = await _context.Assignments
          .Where(ce => ce.CandidateId == candidateId)
          .Include(ce => ce.Exam)
          .OrderByDescending(ce => ce.StartTime)
          .ToListAsync();

      var candidateExamViewModels = _mapper.Map<List<CandidateExamViewModel>>(candidateExams);

      var candidate = await _context.Candidates
          .Include(c => c.Job)
          .FirstOrDefaultAsync(c => c.Id == candidateId);

      ViewData["Candidate"] = _mapper.Map<CandidateViewModel>(candidate);
      ViewData["CandidateExams"] = candidateExamViewModels;

      return View();
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
      var candidateExam = await _context.Assignments
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSetManppings)
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
      var allQuestions = candidateExam.Exam.ExamQuestionSetManppings
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
              .ThenInclude(qs => qs.ExamQuestionSetManppings)
          .FirstOrDefaultAsync(q => q.Id == questionId);

      if (questionToReplace == null || !questionToReplace.QuestionSet.ExamQuestionSetManppings.Any(eqs => eqs.ExamId == candidateExam.ExamId))
      {
        return Json(new { success = false, message = "السؤال غير موجود" });
      }

      // البحث عن سؤال بديل من نفس النوع والصعوبة
      // أولاً نحصل على مجموعات الأسئلة المرتبطة بهذا الامتحان
      var questionSetIds = await _context.ExamQuestionSetManppings
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
      candidateExam.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
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
      var candidateExam = await _context.Assignments
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
          CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };
        _context.CandidateAnswers.Add(candidateAnswer);
      }
      else
      {
        // تحديث حالة العلامة في الإجابة الموجودة
        candidateAnswer.IsFlagged = model.IsFlagged;
        candidateAnswer.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        _context.Update(candidateAnswer);
      }

      await _context.SaveChangesAsync();

      return Ok(new { success = true, message = model.IsFlagged ? "تم تعليم السؤال للمراجعة لاحقاً." : "تم إلغاء تعليم السؤال." });
    }

    // البحث عن السؤال التالي غير المُجاب عليه (إذا كان هناك أي)
    private async Task<Question> GetNextUnansweredQuestion(int candidateExamId)
    {
      var candidateExam = await _context.Assignments
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSetManppings)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
      .Include(ce => ce.CandidateAnswers)
      .FirstOrDefaultAsync(ce => ce.Id == candidateExamId);

      if (candidateExam == null)
      {
        return null;
      }

      // جمع جميع الأسئلة من جميع مجموعات الأسئلة في الامتحان
      var allQuestions = candidateExam.Exam.ExamQuestionSetManppings
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
      var candidateExam = await _context.Assignments
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSetManppings)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
                          .ThenInclude(q => q.Options)
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSetManppings)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
                          .ThenInclude(q => q.MatchingPairs)
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSetManppings)
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
      var allQuestions = candidateExam.Exam.ExamQuestionSetManppings
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
        questionViewModel.TrueFalseAnswer = candidateAnswer != null && candidateAnswer.TrueFalseAnswer.HasValue;
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
          .Include(e => e.ExamQuestionSetManppings)
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
      var candidateExam = await _context.Assignments
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSetManppings)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
          .FirstOrDefaultAsync(ce => ce.Id == candidateExamId);

      if (candidateExam == null)
      {
        return new List<Question>();
      }

      // جمع جميع الأسئلة من جميع مجموعات الأسئلة في الامتحان
      var allQuestions = candidateExam.Exam.ExamQuestionSetManppings
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
      var candidateExam = await _context.Assignments
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.ExamQuestionSetManppings)
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
      var allQuestions = candidateExam.Exam.ExamQuestionSetManppings
          .SelectMany(eqs => eqs.QuestionSet.Questions)
          .ToList();

      // حساب عدد الإجابات الصحيحة
      int correctAnswers = candidateExam.CandidateAnswers.Count(ca => ca.IsCorrect.HasValue && ca.IsCorrect.Value);

      // حساب النتيجة النهائية
      decimal totalQuestions = allQuestions.Count;
      decimal score = totalQuestions > 0 ? (correctAnswers / totalQuestions) * 100 : 0;

      // تحديث حالة الاختبار ونتيجته
      candidateExam.Score = score;
      candidateExam.EndTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
      candidateExam.Status = CandidateExamStatus.Completed.ToString();
      candidateExam.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

      await _context.SaveChangesAsync();

      return Ok(new
      {
        success = true,
        score = score,
        redirectUrl = Url.Action("Results", new { id = candidateExamId })
      });
    }

    private List<string> GetExamRules()
    {
      return new List<string>
      {
        "يجب الإجابة على جميع الأسئلة قبل إنهاء الامتحان",
        "لا يسمح بالخروج من الصفحة أثناء الامتحان",
        "لا يسمح بفتح نوافذ أو تطبيقات أخرى أثناء الامتحان",
        "يجب إنهاء الامتحان خلال المدة المحددة",
        "لا يسمح بالعودة لتعديل الإجابات بعد الانتقال للسؤال التالي",
        "يتم حفظ إجاباتك تلقائياً أثناء التنقل بين الأسئلة",
        "في حالة انقطاع الاتصال، يمكنك متابعة الامتحان من حيث توقفت",
        "التأكد من استقرار الاتصال بالإنترنت قبل البدء"
      };
    }

    private List<string> GetTechnicalInstructions()
    {
      return new List<string>
      {
        "تأكد من استخدام متصفح حديث (Chrome، Firefox، Safari، Edge)",
        "تأكد من تفعيل JavaScript في متصفحك",
        "أغلق جميع التطبيقات غير الضرورية لضمان الأداء الأمثل",
        "تأكد من شحن جهازك أو توصيله بمصدر طاقة",
        "تأكد من استقرار اتصال الإنترنت",
        "استخدم شاشة كبيرة إن أمكن لتجربة أفضل",
        "تأكد من وضوح الصوت إذا كان الامتحان يتضمن محتوى صوتي",
        "تجنب استخدام الجهاز لأغراض أخرى أثناء الامتحان"
      };
    }
  }
}
