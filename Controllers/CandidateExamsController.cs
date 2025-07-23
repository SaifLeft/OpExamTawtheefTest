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
using TawtheefTest.Services;

namespace TawtheefTest.Controllers
{
  public class CandidateExamsController : Controller
  {
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;
    private readonly ICandidateSessionService _sessionService;
    private readonly IAssignmentService _assignmentService;
    private readonly IAnswerService _answerService;
    private readonly IExamCalculationService _calculationService;
    private readonly IQuestionService _questionService;

    public CandidateExamsController(
        ApplicationDbContext context,
        IMapper mapper,
        INotificationService notificationService,
        ICandidateSessionService sessionService,
        IAssignmentService assignmentService,
        IAnswerService answerService,
        IExamCalculationService calculationService,
        IQuestionService questionService)
    {
      _context = context;
      _mapper = mapper;
      _notificationService = notificationService;
      _sessionService = sessionService;
      _assignmentService = assignmentService;
      _answerService = answerService;
      _calculationService = calculationService;
      _questionService = questionService;
    }

    // GET: Assignments
    public async Task<IActionResult> Index()
    {
      if (!_sessionService.IsCandidateLoggedIn())
      {
        TempData["ErrorMessage"] = "يرجى تسجيل الدخول أولاً.";
        return RedirectToAction("Login", "Auth");
      }

      var candidate = await _sessionService.GetCurrentCandidateAsync();
      if (candidate == null)
      {
        _sessionService.ClearSession();
        TempData["ErrorMessage"] = "لم يتم العثور على بيانات المرشح.";
        return RedirectToAction("Login", "Auth");
      }

      var candidateViewModel = _mapper.Map<CandidateViewModel>(candidate);

      // Get candidate assignments
      var assignments = await _assignmentService.GetCandidateAssignmentsAsync(candidate.Id);
      var assignmentViewModels = _mapper.Map<List<AssignmentViewModel>>(assignments);

      // Get available exams for candidate's job
      var exams = await _context.Exams
          .Include(e => e.Job)
          .Where(e => e.JobId == candidate.JobId && e.Status == nameof(ExamStatus.Published))
          .ToListAsync();

      var examViewModels = _mapper.Map<List<ExamForCandidateViewModel>>(exams);

      ViewData["Candidate"] = candidateViewModel;
      ViewData["Assignments"] = assignmentViewModels;

      return View(examViewModels);
    }

    public async Task<IActionResult> ByCandidateId(int id)
    {
      var assignments = await _assignmentService.GetCandidateAssignmentsAsync(id);
      var assignmentViewModels = _mapper.Map<List<AssignmentViewModel>>(assignments);
      ViewData["Assignments"] = assignmentViewModels;
      return View();
    }

    // GET: Assignments/Start/5 - Show instructions page before starting exam
    [HttpGet]
    public async Task<IActionResult> Start(int id)
    {
      var candidateId = _sessionService.GetCurrentCandidateId();
      if (candidateId == null)
      {
        TempData["ErrorMessage"] = "يرجى تسجيل الدخول أولاً.";
        return RedirectToAction("Login", "Auth");
      }

      // Get exam details
      var exam = await _context.Exams
          .Include(e => e.Job)
          .Include(e => e.ExamQuestionSetMappings)
              .ThenInclude(eqs => eqs.QuestionSet)
                  .ThenInclude(qs => qs.Questions)
          .FirstOrDefaultAsync(e => e.Id == id);

      if (exam == null)
      {
        TempData["ErrorMessage"] = "الامتحان غير موجود.";
        return RedirectToAction("Index");
      }

      var candidate = await _sessionService.GetCurrentCandidateAsync();
      if (candidate == null)
      {
        _sessionService.ClearSession();
        TempData["ErrorMessage"] = "لم يتم العثور على بيانات المرشح.";
        return RedirectToAction("Login", "Auth");
      }

      // Validate exam is for candidate's job
      if (exam.JobId != candidate.JobId)
      {
        TempData["ErrorMessage"] = "هذا الامتحان غير متاح لوظيفتك.";
        return RedirectToAction("Index");
      }

      // Check if candidate has completed this exam
      var completedExam = await _assignmentService.GetCompletedAssignmentAsync(candidateId.Value, id);
      if (completedExam != null)
      {
        TempData["ErrorMessage"] = "لقد أكملت هذا الاختبار بالفعل.";
        return RedirectToAction(nameof(Results), new { id = completedExam.Id });
      }

      // Check for existing attempt
      var existingAttempt = await _assignmentService.GetExistingAttemptAsync(candidateId.Value, id);

      // Calculate total questions
      var allQuestions = exam.ExamQuestionSetMappings
          .SelectMany(eqs => eqs.QuestionSet.Questions)
          .ToList();

      var totalQuestions = exam.TotalQuestionsPerCandidate > 0 ? exam.TotalQuestionsPerCandidate : allQuestions.Count;

      // Create view model
      var viewModel = new ExamInstructionsViewModel
      {
        ExamId = exam.Id,
        ExamName = exam.Name,
        JobName = exam.Job.Title,
        Duration = exam.Duration,
        TotalQuestions = totalQuestions,
        CandidateName = candidate.Name,
        HasExistingAttempt = existingAttempt != null,
        ExistingAttemptId = existingAttempt?.Id,
        ExamRules = GetExamRules(),
        TechnicalInstructions = GetTechnicalInstructions()
      };

      return View(viewModel);
    }

    // POST: Assignments/StartExam/5 - Actually start the exam
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartExam(int id)
    {
      var candidateId = _sessionService.GetCurrentCandidateId();
      if (candidateId == null)
      {
        TempData["ErrorMessage"] = "يرجى تسجيل الدخول أولاً.";
        return RedirectToAction("Login", "Auth");
      }

      // Get exam details
      var exam = await _context.Exams
          .Include(e => e.Job)
          .Include(e => e.ExamQuestionSetMappings)
              .ThenInclude(eqs => eqs.QuestionSet)
                  .ThenInclude(qs => qs.Questions)
          .FirstOrDefaultAsync(e => e.Id == id);

      if (exam == null)
      {
        TempData["ErrorMessage"] = "الامتحان غير موجود.";
        return RedirectToAction("Index");
      }

      // Check if exam has questions
      var allQuestions = exam.ExamQuestionSetMappings
          .SelectMany(eqs => eqs.QuestionSet.Questions)
          .ToList();

      if (allQuestions.Count == 0)
      {
        TempData["ErrorMessage"] = "لا توجد أسئلة في هذا الامتحان.";
        return RedirectToAction("Index");
      }

      // Check if candidate has completed this exam
      var completedExam = await _assignmentService.GetCompletedAssignmentAsync(candidateId.Value, id);
      if (completedExam != null)
      {
        TempData["ErrorMessage"] = "لقد أكملت هذا الاختبار بالفعل.";
        return RedirectToAction(nameof(Results), new { id = completedExam.Id });
      }

      // Check for existing attempt
      var existingAttempt = await _assignmentService.GetExistingAttemptAsync(candidateId.Value, id);
      if (existingAttempt != null)
      {
        TempData["InfoMessage"] = "أنت تقوم بمتابعة محاولة اختبار سابقة.";
        return RedirectToAction(nameof(Take), new { id = existingAttempt.Id });
      }

      // Create new assignment
      var newAssignment = await _assignmentService.CreateAssignmentAsync(candidateId.Value, id);
      if (newAssignment == null)
      {
        TempData["ErrorMessage"] = "حدث خطأ في إنشاء الاختبار.";
        return RedirectToAction("Index");
      }

      TempData["SuccessMessage"] = "تم بدء الاختبار بنجاح.";
      return RedirectToAction(nameof(Take), new { id = newAssignment.Id });
    }

    // GET: Assignments/Take/5
    public async Task<IActionResult> Take(int id)
    {
      var candidateId = _sessionService.GetCurrentCandidateId();
      if (candidateId == null)
      {
        TempData["ErrorMessage"] = "يرجى تسجيل الدخول أولاً.";
        return RedirectToAction("Login", "Auth");
      }

      var assignment = await _assignmentService.GetAssignmentWithDetailsAsync(id, candidateId.Value);
      if (assignment == null)
      {
        TempData["ErrorMessage"] = "الاختبار غير موجود.";
        return RedirectToAction("Index");
      }

      // Check assignment status
      if (assignment.Status != AssignmentStatus.InProgress.ToString())
      {
        if (assignment.Status == AssignmentStatus.Completed.ToString())
        {
          return RedirectToAction(nameof(Results), new { id = assignment.Id });
        }
        else
        {
          return RedirectToAction(nameof(Start), new { id = assignment.ExamId });
        }
      }

      // Get all questions for the exam
      var allQuestions = await _questionService.GetQuestionsForAssignmentAsync(id);

      // Get candidate answers
      var candidateAnswers = assignment.CandidateAnswers.ToList();

      // Calculate totals
      var totalQuestions = assignment.TotalQuestions > 0 ? assignment.TotalQuestions : allQuestions.Count;
      var completedQuestions = assignment.CompletedQuestions > 0 ? assignment.CompletedQuestions : 0;

      // Create assignment view model
      var assignmentViewModel = new AssignmentViewModel()
      {
        Id = assignment.Id,
        ExamId = assignment.ExamId,
        JobTitle = assignment.Exam.Job.Title,
        CandidateName = assignment.Candidate.Name,
        CandidateId = assignment.CandidateId,
        StartTime = assignment.StartTime,
        EndTime = assignment.EndTime,
        Duration = assignment.Exam.Duration,
        TotalQuestions = totalQuestions,
        CompletedQuestions = completedQuestions,
        Score = assignment.Score,
        Status = assignment.Status,
        FlaggedQuestions = candidateAnswers.Where(ca => ca.IsFlagged).Select(ca => ca.QuestionId).ToList(),
        RemainingTime = _calculationService.CalculateRemainingTime(assignment)
      };

      // Load questions
      assignmentViewModel.Questions = _mapper.Map<List<CandidateQuestionViewModel>>(allQuestions);

      ViewData["CandidateAnswers"] = candidateAnswers;
      ViewData["ProgressPercentage"] = _calculationService.CalculateProgressPercentage(assignment, candidateAnswers);

      return View(assignmentViewModel);
    }

    // POST: Assignments/SaveAnswer
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAnswer(SaveAnswerDTO model)
    {
      var candidateId = _sessionService.GetCurrentCandidateId();
      if (candidateId == null)
      {
        return Unauthorized(new { success = false, message = "يرجى تسجيل الدخول أولاً." });
      }

      var result = await _answerService.SaveAnswerAsync(model, candidateId.Value);
      if (result == null)
      {
        return BadRequest(new { success = false, message = "خطأ في حفظ الإجابة." });
      }

      return Ok(new { success = true, message = "تم حفظ الإجابة بنجاح." });
    }

    // POST: Assignments/Submit
    [HttpPost]
    public async Task<IActionResult> Submit(int id)
    {
      var candidateId = _sessionService.GetCurrentCandidateId();
      if (candidateId == null)
      {
        TempData["ErrorMessage"] = "يرجى تسجيل الدخول أولاً.";
        return RedirectToAction("Login", "Auth");
      }

      var assignment = await _assignmentService.CompleteAssignmentAsync(id, candidateId.Value);
      if (assignment == null)
      {
        return NotFound();
      }

      if (assignment.Status == AssignmentStatus.Completed.ToString())
      {
        TempData["SuccessMessage"] = "تم تسليم الاختبار بنجاح";
        return RedirectToAction(nameof(Results), new { id = assignment.Id });
      }

      TempData["ErrorMessage"] = "حدث خطأ في تسليم الاختبار.";
      return RedirectToAction(nameof(Take), new { id });
    }

    // GET: Assignments/Results/5
    public async Task<IActionResult> Results(int id)
    {
      var candidateId = _sessionService.GetCurrentCandidateId();
      if (candidateId == null)
      {
        TempData["ErrorMessage"] = "يرجى تسجيل الدخول أولاً.";
        return RedirectToAction("Login", "Auth");
      }

      var assignment = await _assignmentService.GetAssignmentWithDetailsAsync(id, candidateId.Value);
      if (assignment == null)
      {
        return NotFound();
      }

      var assignmentResult = _mapper.Map<AssignmentResultViewModel>(assignment);

      // Get all questions from exam
      var allQuestions = assignment.Exam.ExamQuestionSetMappings
          .SelectMany(eqs => eqs.QuestionSet.Questions)
          .ToList();

      // Order answers by question display order
      assignmentResult.Answers = _mapper.Map<List<CandidateAnswerViewModel>>(
          assignment.CandidateAnswers.OrderBy(ca => ca.Question.DisplayOrder));

      ViewData["CandidateAnswers"] = assignmentResult.Answers;

      return View(assignmentResult);
    }

    // GET: Assignments/MyExams
    public async Task<IActionResult> MyExams()
    {
      var candidateId = _sessionService.GetCurrentCandidateId();
      if (candidateId == null)
      {
        TempData["ErrorMessage"] = "يرجى تسجيل الدخول أولاً.";
        return RedirectToAction("Login", "Auth");
      }

      var assignments = await _assignmentService.GetCandidateAssignmentsAsync(candidateId.Value);
      var assignmentViewModels = _mapper.Map<List<AssignmentViewModel>>(assignments);

      var candidate = await _sessionService.GetCurrentCandidateAsync();
      ViewData["Candidate"] = _mapper.Map<CandidateViewModel>(candidate);
      ViewData["Assignments"] = assignmentViewModels;

      return View();
    }

    // POST: Assignments/ReplaceQuestion
    [HttpPost]
    public async Task<IActionResult> ReplaceQuestion(int AssignmentId, int questionId)
    {
      var candidateId = _sessionService.GetCurrentCandidateId();
      if (candidateId == null)
      {
        return Unauthorized(new { success = false, message = "يرجى تسجيل الدخول أولاً." });
      }

      // Get assignment and validate access
      var assignment = await _assignmentService.GetAssignmentWithDetailsAsync(AssignmentId, candidateId.Value);
      if (assignment == null || assignment.Status == AssignmentStatus.Completed.ToString())
      {
        return BadRequest(new { success = false, message = "الاختبار غير متاح." });
      }

      // Check if question was already replaced
      if (assignment.QuestionReplaced)
      {
        return BadRequest(new { success = false, message = "لا يمكنك استبدال أكثر من سؤال واحد." });
      }

      // Get all questions for the assignment
      var allQuestions = await _questionService.GetQuestionsForAssignmentAsync(AssignmentId);
      if (allQuestions.Count <= 1)
      {
        return BadRequest(new { success = false, message = "لا يمكنك استبدال السؤال إذا كان هناك سؤال واحد فقط." });
      }

      // Find replacement question
      var replacementQuestion = await _questionService.FindReplacementQuestionAsync(AssignmentId, questionId);
      if (replacementQuestion == null)
      {
        return Json(new { success = false, message = "لا يوجد سؤال بديل متاح" });
      }

      // Remove existing answer if exists
      var existingAnswer = await _answerService.GetCandidateAnswerAsync(AssignmentId, questionId);
      if (existingAnswer != null)
      {
        _context.CandidateAnswers.Remove(existingAnswer);
      }

      // Update assignment to mark question as replaced
      assignment.QuestionReplaced = true;
      assignment.UpdatedAt = DateTime.Now;
      _context.Update(assignment);

      await _context.SaveChangesAsync();

      // Create notification
      await _notificationService.CreateNotificationAsync(
          candidateId.Value,
          "تم استبدال سؤال",
          $"تم استبدال السؤال بنجاح في الاختبار {assignment.Exam.Name}.",
          "info"
      );

      return Json(new { success = true, replacementQuestionId = replacementQuestion.Id });
    }

    // POST: Assignments/FlagQuestion
    [HttpPost]
    public async Task<IActionResult> FlagQuestion(QuestionFlagDTO model)
    {
      var candidateId = _sessionService.GetCurrentCandidateId();
      if (candidateId == null)
      {
        return Unauthorized(new { success = false, message = "يرجى تسجيل الدخول أولاً." });
      }

      var result = await _answerService.FlagQuestionAsync(model, candidateId.Value);
      if (result == null)
      {
        return NotFound(new { success = false, message = "لم يتم العثور على الاختبار أو السؤال." });
      }

      return Ok(new
      {
        success = true,
        message = model.IsFlagged ? "تم تعليم السؤال للمراجعة لاحقاً." : "تم إلغاء تعليم السؤال."
      });
    }

    // GET: Get question by index
    [HttpGet]
    public async Task<IActionResult> GetQuestion(int AssignmentId, int questionIndex)
    {
      var candidateId = _sessionService.GetCurrentCandidateId();
      if (candidateId == null)
      {
        return Unauthorized(new { success = false, message = "يرجى تسجيل الدخول أولاً." });
      }

      // Validate assignment access
      if (!await _assignmentService.ValidateAssignmentAccessAsync(AssignmentId, candidateId.Value))
      {
        return NotFound(new { success = false, message = "الاختبار غير موجود." });
      }

      // Get all questions for assignment
      var allQuestions = await _questionService.GetQuestionsForAssignmentAsync(AssignmentId);

      // Validate question index
      if (questionIndex < 0 || questionIndex >= allQuestions.Count)
      {
        return BadRequest(new { success = false, message = "رقم السؤال غير صالح." });
      }

      var question = allQuestions[questionIndex];

      // Get candidate answer
      var candidateAnswer = await _answerService.GetCandidateAnswerAsync(AssignmentId, question.Id);

      var questionViewModel = _mapper.Map<CandidateQuestionViewModel>(question);
      questionViewModel.IsAnswered = candidateAnswer != null && !string.IsNullOrEmpty(candidateAnswer.AnswerText);
      questionViewModel.IsFlagged = candidateAnswer != null && candidateAnswer.IsFlagged;

      // Set question type specific properties
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

    // POST: Assignments/Create
    [HttpPost]
    public async Task<IActionResult> Create(int examId)
    {
      var candidateId = _sessionService.GetCurrentCandidateId();
      if (candidateId == null)
      {
        TempData["ErrorMessage"] = "يرجى تسجيل الدخول أولاً.";
        return RedirectToAction("Login", "Auth");
      }

      // Check if exam exists
      var exam = await _context.Exams
          .Include(e => e.ExamQuestionSetMappings)
              .ThenInclude(eqs => eqs.QuestionSet)
                  .ThenInclude(qs => qs.Questions)
          .FirstOrDefaultAsync(e => e.Id == examId);

      if (exam == null)
      {
        TempData["ErrorMessage"] = "الامتحان غير موجود.";
        return RedirectToAction("Index", "Home");
      }

      return RedirectToAction("Start", new { id = examId });
    }

    // POST: Assignments/Complete
    [HttpPost]
    public async Task<IActionResult> Complete(int AssignmentId)
    {
      var candidateId = _sessionService.GetCurrentCandidateId();
      if (candidateId == null)
      {
        return Unauthorized(new { success = false, message = "يرجى تسجيل الدخول أولاً." });
      }

      var assignment = await _assignmentService.CompleteAssignmentAsync(AssignmentId, candidateId.Value);
      if (assignment == null)
      {
        return BadRequest(new { success = false, message = "الاختبار غير موجود." });
      }

      if (assignment.Status != AssignmentStatus.Completed.ToString())
      {
        return BadRequest(new { success = false, message = "حدث خطأ في إكمال الاختبار." });
      }

      return Ok(new
      {
        success = true,
        score = assignment.Score,
        redirectUrl = Url.Action("Results", new { id = AssignmentId })
      });
    }

    #region Private Helper Methods

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

    #endregion
  }
}
