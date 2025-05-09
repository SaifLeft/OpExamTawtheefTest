using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using TawtheefTest.ViewModels;
using TawtheefTest.DTOs;
using TawtheefTest.Enum;
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
          .Where(e => !candidateExams.Any(ce => ce.ExamId == e.Id && ce.Status == CandidateExamStatus.Completed.ToString()))
          .ToListAsync();

      var examViewModels = _mapper.Map<List<ExamForCandidateViewModel>>(exams);

      ViewData["Candidate"] = candidateViewModel;
      ViewData["CandidateExams"] = candidateExamViewModels;

      return View(examViewModels);
    }

    // GET: CandidateExams/Start/5
    public async Task<IActionResult> Start(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      // التحقق من تسجيل دخول المرشح
      var candidateId = GetCurrentCandidateId();
      if (candidateId == null)
      {
        TempData["ErrorMessage"] = "يرجى تسجيل الدخول أولاً.";
        return RedirectToAction("Login", "Auth");
      }

      // الحصول على تفاصيل الاختبار
      var exam = await _context.Exams
          .Include(e => e.Job)
          .Include(e => e.Questions)
          .FirstOrDefaultAsync(e => e.Id == id);

      if (exam == null)
      {
        return NotFound();
      }

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
      var candidateExam = new CandidateExam
      {
        CandidateId = candidateId.Value,
        ExamId = id.Value,
        StartTime = DateTime.UtcNow,
        Status = CandidateExamStatus.InProgress.ToString(),
        CreatedAt = DateTime.UtcNow,
        TotalQuestions = exam.Questions.Count,
        CompletedQuestions = 0
      };

      _context.CandidateExams.Add(candidateExam);
      await _context.SaveChangesAsync();

      // إنشاء إشعار ببدء الاختبار
      await _notificationService.CreateNotificationAsync(
          candidateId.Value,
          "تم بدء اختبار جديد",
          $"لقد بدأت اختبار {exam.Name} بنجاح. عدد الأسئلة: {exam.Questions.Count}، مدة الاختبار: {exam.Duration} دقيقة.",
          "info"
      );

      TempData["SuccessMessage"] = "تم بدء الاختبار بنجاح.";
      return RedirectToAction(nameof(Take), new { id = candidateExam.Id });
    }

    // GET: CandidateExams/Take/5
    public async Task<IActionResult> Take(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

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
              .ThenInclude(e => e.Questions)
                  .ThenInclude(q => q.Options)
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.Questions)
                  .ThenInclude(q => q.MatchingPairs)
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.Questions)
                  .ThenInclude(q => q.OrderingItems)
          .Include(ce => ce.CandidateAnswers)
          .FirstOrDefaultAsync(ce => ce.Id == id && ce.CandidateId == candidateId);

      if (candidateExam == null)
      {
        return NotFound();
      }

      if (candidateExam.Status == CandidateExamStatus.Completed.ToString())
      {
        return RedirectToAction(nameof(Results), new { id = candidateExam.Id });
      }

      // الحصول على إجابات المرشح
      var candidateAnswers = await _context.CandidateAnswers
          .Include(ca => ca.Question)
          .Where(ca => ca.CandidateExamId == id)
          .ToListAsync();

      var candidateExamViewModel = _mapper.Map<CandidateExamViewModel>(candidateExam);
      var candidateAnswerDTOs = _mapper.Map<List<CandidateAnswerDTO>>(candidateAnswers);

      // إعداد بيانات إضافية للعرض
      var remainingTime = CalculateRemainingTime(candidateExam);
      candidateExamViewModel.RemainingTime = remainingTime;

      ViewData["CandidateAnswers"] = candidateAnswerDTOs;
      ViewData["ProgressPercentage"] = CalculateProgressPercentage(candidateExam, candidateAnswers);

      return View(candidateExamViewModel);
    }

    // حساب النسبة المئوية للتقدم في الاختبار
    private int CalculateProgressPercentage(CandidateExam candidateExam, List<CandidateAnswer> candidateAnswers)
    {
      if (candidateExam.Exam == null || candidateExam.Exam.Questions == null || candidateExam.Exam.Questions.Count == 0)
      {
        return 0;
      }

      int totalQuestions = candidateExam.TotalQuestions > 0 ? candidateExam.TotalQuestions : candidateExam.Exam.Questions.Count;
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
    public async Task<IActionResult> SaveAnswer(int candidateExamId, int questionId, string answer)
    {
      // التحقق من تسجيل دخول المرشح
      var candidateId = GetCurrentCandidateId();
      if (candidateId == null)
      {
        return Unauthorized(new { success = false, message = "يرجى تسجيل الدخول أولاً." });
      }

      // الحصول على اختبار المرشح
      var candidateExam = await _context.CandidateExams
          .FirstOrDefaultAsync(ce => ce.Id == candidateExamId && ce.CandidateId == candidateId);

      if (candidateExam == null || candidateExam.Status == CandidateExamStatus.Completed.ToString())
      {
        return BadRequest(new { success = false, message = "الاختبار غير متاح." });
      }

      // الحصول على السؤال
      var question = await _context.Questions
          .Include(q => q.Options)
          .Include(q => q.MatchingPairs)
          .Include(q => q.OrderingItems)
          .FirstOrDefaultAsync(q => q.Id == questionId);

      if (question == null)
      {
        return BadRequest(new { success = false, message = "السؤال غير موجود." });
      }

      // التحقق مما إذا كانت الإجابة موجودة بالفعل
      var existingAnswer = await _context.CandidateAnswers
          .FirstOrDefaultAsync(ca => ca.CandidateExamId == candidateExamId && ca.QuestionId == questionId);

      bool? isCorrect = null;

      // التحقق مما إذا كانت الإجابة صحيحة
      switch (question.QuestionType)
      {
        case "MCQ":
          var selectedOption = question.Options.FirstOrDefault(o => o.Id.ToString() == answer);
          isCorrect = selectedOption?.IsCorrect;
          break;

        case "TF":
          if (bool.TryParse(answer, out bool boolAnswer))
          {
            isCorrect = boolAnswer == question.TrueFalseAnswer;
          }
          break;

        case "ShortAnswer":
        case "FillInTheBlank":
          isCorrect = string.Equals(question.Answer, answer, StringComparison.OrdinalIgnoreCase);
          break;

        case "MultiSelect":
          try
          {
            var selectedOptions = JsonSerializer.Deserialize<List<int>>(answer);
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
            var pairs = JsonSerializer.Deserialize<Dictionary<string, string>>(answer);
            var correctPairs = question.MatchingPairs.ToDictionary(p => p.LeftItem, p => p.RightItem);
            isCorrect = pairs != null && pairs.OrderBy(x => x.Key).SequenceEqual(correctPairs.OrderBy(x => x.Key));
          }
          catch
          {
            isCorrect = false;
          }
          break;

        case "Ordering":
          try
          {
            var orderItems = JsonSerializer.Deserialize<List<string>>(answer);
            var correctOrder = question.OrderingItems.OrderBy(o => o.CorrectOrder).Select(o => o.Text).ToList();
            isCorrect = orderItems != null && orderItems.SequenceEqual(correctOrder);
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
        existingAnswer.AnswerText = answer;
        existingAnswer.IsCorrect = isCorrect;
        existingAnswer.UpdatedAt = DateTime.UtcNow;
        _context.Update(existingAnswer);
      }
      else
      {
        // إنشاء إجابة جديدة
        var candidateAnswer = new CandidateAnswer
        {
          CandidateExamId = candidateExamId,
          QuestionId = questionId,
          AnswerText = answer,
          IsCorrect = isCorrect,
          CreatedAt = DateTime.UtcNow
        };

        _context.CandidateAnswers.Add(candidateAnswer);

        // تحديث عدد الأسئلة المجابة
        candidateExam.CompletedQuestions = await _context.CandidateAnswers
            .Where(ca => ca.CandidateExamId == candidateExamId)
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
              .ThenInclude(e => e.Questions)
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
      var totalQuestions = candidateExam.TotalQuestions > 0 ? candidateExam.TotalQuestions : candidateExam.Exam.Questions.Count;
      var answeredQuestions = candidateExam.CandidateAnswers.Select(ca => ca.QuestionId).Distinct().Count();
      var correctAnswers = candidateExam.CandidateAnswers.Count(ca => ca.IsCorrect == true);
      var score = totalQuestions > 0 ? (decimal)correctAnswers / totalQuestions * 100 : 0;

      // تحديث اختبار المرشح
      candidateExam.EndTime = DateTime.UtcNow;
      candidateExam.Score = Math.Round(score, 2);
      candidateExam.Status = CandidateExamStatus.Completed.ToString();
      candidateExam.UpdatedAt = DateTime.UtcNow;
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
    public async Task<IActionResult> Results(int? id)
    {
      if (id == null)
      {
        return RedirectToAction(nameof(Index));
      }

      // التحقق من تسجيل دخول المرشح
      var candidateId = GetCurrentCandidateId();
      if (candidateId == null)
      {
        TempData["ErrorMessage"] = "يرجى تسجيل الدخول أولاً.";
        return RedirectToAction("Login", "Auth");
      }

      var candidateExam = await _context.CandidateExams
          .Include(ce => ce.Candidate)
          .Include(ce => ce.Exam)
          .Include(ce => ce.CandidateAnswers)
              .ThenInclude(ca => ca.Question)
                  .ThenInclude(q => q.Options)
          .FirstOrDefaultAsync(ce => ce.Id == id && ce.CandidateId == candidateId);

      if (candidateExam == null)
      {
        return NotFound();
      }

      var candidateExamResult = _mapper.Map<CandidateExamResultViewModel>(candidateExam);

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

      var candidateExams = await _context.CandidateExams
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
        return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });
      }

      // الحصول على اختبار المرشح
      var candidateExam = await _context.CandidateExams
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.Questions)
          .FirstOrDefaultAsync(ce => ce.Id == candidateExamId && ce.CandidateId == candidateId);

      if (candidateExam == null)
      {
        return Json(new { success = false, message = "الاختبار غير موجود" });
      }

      if (candidateExam.Status == CandidateExamStatus.Completed.ToString())
      {
        return Json(new { success = false, message = "الاختبار مكتمل" });
      }

      // التحقق مما إذا كان استبدال السؤال مستخدمًا بالفعل
      if (candidateExam.QuestionReplaced)
      {
        return Json(new { success = false, message = "تم استخدام استبدال السؤال بالفعل" });
      }

      // الحصول على السؤال المراد استبداله
      var questionToReplace = await _context.Questions
          .FirstOrDefaultAsync(q => q.Id == questionId && q.ExamId == candidateExam.ExamId);

      if (questionToReplace == null)
      {
        return Json(new { success = false, message = "السؤال غير موجود" });
      }

      // البحث عن سؤال بديل من نفس النوع والصعوبة
      var replacementQuestion = await _context.Questions
          .Where(q => q.ExamId == candidateExam.ExamId &&
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

    // GET: CandidateExams/GetQuestion/5/2
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
              .ThenInclude(e => e.Questions)
                  .ThenInclude(q => q.Options)
          .FirstOrDefaultAsync(ce => ce.Id == candidateExamId && ce.CandidateId == candidateId);

      if (candidateExam == null)
      {
        return NotFound(new { success = false, message = "لم يتم العثور على الاختبار." });
      }

      if (candidateExam.Status == CandidateExamStatus.Completed.ToString())
      {
        return BadRequest(new { success = false, message = "تم إكمال هذا الاختبار بالفعل." });
      }

      // الحصول على سؤال بناءً على الفهرس
      var allQuestions = candidateExam.Exam.Questions.OrderBy(q => q.DisplayOrder).ToList();
      if (questionIndex < 0 || questionIndex >= allQuestions.Count)
      {
        return BadRequest(new { success = false, message = "مؤشر السؤال غير صالح." });
      }

      var question = allQuestions[questionIndex];

      // الحصول على إجابة المرشح لهذا السؤال إن وجدت
      var candidateAnswer = await _context.CandidateAnswers
          .FirstOrDefaultAsync(ca => ca.CandidateExamId == candidateExamId && ca.QuestionId == question.Id);

      var questionViewModel = _mapper.Map<CandidateQuestionViewModel>(question);
      questionViewModel.IsAnswered = candidateAnswer != null && !string.IsNullOrEmpty(candidateAnswer.AnswerText);
      questionViewModel.IsFlagged = candidateAnswer != null && candidateAnswer.IsFlagged;

      return Ok(new
      {
        success = true,
        question = questionViewModel,
        currentIndex = questionIndex,
        totalQuestions = allQuestions.Count,
        answer = candidateAnswer?.AnswerText
      });
    }
  }
}
