using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using TawtheefTest.Data.Structure;
using TawtheefTest.DTOs;
using TawtheefTest.DTOs.ExamModels;
using TawtheefTest.Enums;
using TawtheefTest.Services;
using TawtheefTest.Services.Exams;
using TawtheefTest.ViewModels;

namespace TawtheefTest.Controllers
{
  public class ExamsController : Controller
  {
    private readonly ApplicationDbContext _context;
    private readonly IExamService _examService;
    private readonly IQuestionSetService _questionSetService;
    private readonly IQuestionManagementService _questionManagementService;
    private readonly IExamPublishingService _examPublishingService;
    private readonly IExamValidationService _examValidationService;
    private readonly IExamUtilityService _examUtilityService;

    public ExamsController(
        ApplicationDbContext context,
        IExamService examService,
        IQuestionSetService questionSetService,
        IQuestionManagementService questionManagementService,
        IExamPublishingService examPublishingService,
        IExamValidationService examValidationService,
        IExamUtilityService examUtilityService)
    {
      _context = context;
      _examService = examService;
      _questionSetService = questionSetService;
      _questionManagementService = questionManagementService;
      _examPublishingService = examPublishingService;
      _examValidationService = examValidationService;
      _examUtilityService = examUtilityService;
    }

    // GET: Exams
    public async Task<IActionResult> Index()
    {
      var exams = await _examService.GetAllExamsAsync();
      return View(exams);
    }

    // GET: Exams/Details/5
    public async Task<IActionResult> Details(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var examDetails = await _examService.GetExamDetailsAsync(id.Value);
      if (examDetails == null)
      {
        return NotFound();
      }

      // Get questions for the exam
      var questions = await _questionManagementService.GetExamQuestionsAsync(id.Value);
      ViewBag.Questions = questions;

      // Debug information for status
      TempData["StatusDebug"] = $"قيمة Status في الـ DTO: {examDetails.Status}";

      return View(examDetails);
    }

    // GET: Exams/Create
    public IActionResult Create()
    {
      ViewBag.Jobs = new SelectList(_context.Jobs, "Id", "Title");
      return View();
    }

    // POST: Exams/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateExamViewModel model)
    {
      if (ModelState.IsValid)
      {
        var exam = await _examService.CreateExamAsync(model);
        if (exam != null)
        {
          TempData["SuccessMessage"] = "تم إنشاء الاختبار بنجاح";
          return RedirectToAction(nameof(Details), new { id = exam.Id });
        }

        ModelState.AddModelError("", "حدث خطأ أثناء إنشاء الاختبار");
      }

      ViewBag.Jobs = new SelectList(_context.Jobs, "Id", "Title", model.JobId);
      return View(model);
    }

    // GET: Exams/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var exam = await _examService.GetExamByIdAsync(id.Value);
      if (exam == null)
      {
        return NotFound();
      }

      var examDto = new EditExamDTO
      {
        Id = exam.Id,
        Name = exam.Name,
        JobId = exam.JobId,
        Duration = exam.Duration,
        StartDate = exam.StartDate,
        EndDate = exam.EndDate,
        ShowResultsImmediately = exam.ShowResultsImmediately,
        SendExamLinkToApplicants = exam.SendExamLinkToApplicants
      };

      ViewBag.Jobs = new SelectList(_context.Jobs, "Id", "Title", exam.JobId);
      return View(examDto);
    }

    // POST: Exams/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditExamDTO examDto)
    {
      if (id != examDto.Id)
      {
        return NotFound();
      }

      if (ModelState.IsValid)
      {
        try
        {
          var updatedExam = await _examService.UpdateExamAsync(id, examDto);
          if (updatedExam != null)
          {
            TempData["SuccessMessage"] = "تم تحديث الاختبار بنجاح";
            return RedirectToAction(nameof(Details), new { id });
          }

          return NotFound();
        }
        catch (DbUpdateConcurrencyException)
        {
          if (!await _examService.ExamExistsAsync(examDto.Id))
          {
            return NotFound();
          }
          else
          {
            throw;
          }
        }
      }

      ViewBag.Jobs = new SelectList(_context.Jobs, "Id", "Title", examDto.JobId);
      return View(examDto);
    }

    // GET: Exams/Results/5
    public async Task<IActionResult> Results(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var results = await _examService.GetExamResultsAsync(id.Value);
      if (results == null || !results.Any())
      {
        return NotFound();
      }

      var exam = await _examService.GetExamByIdAsync(id.Value);
      ViewBag.ExamName = exam?.Name;
      ViewBag.ExamId = exam?.Id;

      return View(results);
    }

    // GET: Exams/ByJob/5
    public async Task<IActionResult> ByJob(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var job = await _context.Jobs.FindAsync(id);
      if (job == null)
      {
        return NotFound();
      }

      var exams = await _examService.GetExamsByJobAsync(id.Value);

      ViewBag.JobName = job.Title;
      ViewBag.JobId = job.Id;

      return View(exams);
    }

    // GET: Exams/ExamQuestions/5
    public async Task<IActionResult> ExamQuestions(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var exam = await _examService.GetExamByIdAsync(id.Value);
      if (exam == null)
      {
        return NotFound();
      }

      var questions = await _questionManagementService.GetExamQuestionsAsync(id.Value);

      if (!questions.Any())
      {
        TempData["WarningMessage"] = "لا توجد أسئلة في هذا الاختبار بعد. يرجى إضافة بعض الأسئلة أولاً.";
        return RedirectToAction(nameof(Details), new { id });
      }

      ViewBag.ExamId = id;
      ViewBag.ExamName = exam.Name;

      return View(questions);
    }

    // POST: Exams/ToggleShowResults
    [HttpPost]
    public async Task<IActionResult> ToggleShowResults(int id)
    {
      try
      {
        var success = await _examService.ToggleShowResultsAsync(id);
        if (!success)
        {
          return Json(new { success = false, message = "الاختبار غير موجود" });
        }

        var exam = await _examService.GetExamByIdAsync(id);
        return Json(new
        {
          success = true,
          newValue = exam.ShowResultsImmediately,
          message = exam.ShowResultsImmediately ? "تم تفعيل عرض النتائج للمرشحين فوراً" : "تم إلغاء عرض النتائج للمرشحين فوراً"
        });
      }
      catch (Exception)
      {
        return Json(new { success = false, message = "حدث خطأ أثناء تحديث الإعداد" });
      }
    }

    // POST: Exams/ToggleExamLinks
    [HttpPost]
    public async Task<IActionResult> ToggleExamLinks(int id)
    {
      try
      {
        var success = await _examService.ToggleExamLinksAsync(id);
        if (!success)
        {
          return Json(new { success = false, message = "الاختبار غير موجود" });
        }

        var exam = await _examService.GetExamByIdAsync(id);
        return Json(new
        {
          success = true,
          newValue = exam.SendExamLinkToApplicants,
          message = exam.SendExamLinkToApplicants ? "تم تفعيل إرسال روابط الاختبار للمرشحين" : "تم إلغاء إرسال روابط الاختبار للمرشحين"
        });
      }
      catch (Exception)
      {
        return Json(new { success = false, message = "حدث خطأ أثناء تحديث الإعداد" });
      }
    }

    // GET: Exams/PublishExam/5
    public async Task<IActionResult> PublishExam(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var publishModel = await _examPublishingService.PrepareExamForPublishingAsync(id.Value);
      if (publishModel == null)
      {
        return NotFound();
      }

      // Validate exam can be published
      var canBePublished = await _examPublishingService.CanExamBePublishedAsync(id.Value);
      if (!canBePublished)
      {
        var validationResult = await _examValidationService.ValidateExamForPublishingAsync(id.Value);
        TempData["ErrorMessage"] = validationResult.ErrorMessage;
        return RedirectToAction(nameof(Details), new { id });
      }

      return View(publishModel);
    }

    // POST: Exams/PublishExam
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPublishExam(PublishExamViewModel model)
    {
      if (!ModelState.IsValid)
      {
        return View("PublishExam", model);
      }

      var result = await _examPublishingService.PublishExamAsync(model);
      if (result.Success)
      {
        TempData["SuccessMessage"] = result.Message;
      }
      else
      {
        TempData["ErrorMessage"] = result.Message;
      }

      return RedirectToAction(nameof(Details), new { id = model.ExamId });
    }

    // GET: Exams/TogglePublishStatus/5
    public async Task<IActionResult> TogglePublishStatus(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var success = await _examService.TogglePublishStatusAsync(id.Value);
      if (!success)
      {
        return NotFound();
      }

      var exam = await _examService.GetExamByIdAsync(id.Value);
      var message = exam.Status == nameof(ExamStatus.Published) ? "تم نشر الاختبار" : "تم إلغاء نشر الاختبار";
      TempData["SuccessMessage"] = message;

      return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> AssignQuestionSetsToExam(int examId, List<int> questionSetIds)
    {
      try
      {
        var success = await _questionSetService.AssignQuestionSetsToExamAsync(examId, questionSetIds);
        if (!success)
        {
          return NotFound(new { success = false, message = "الامتحان غير موجود" });
        }

        return Ok(new { success = true, message = "تم تعيين مجموعات الأسئلة بنجاح" });
      }
      catch (Exception ex)
      {
        return StatusCode(500, new { success = false, message = $"حدث خطأ: {ex.Message}" });
      }
    }

    // POST: Exams/DeleteQuestion/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
      var question = await _questionManagementService.GetQuestionWithDetailsAsync(id);
      if (question == null)
      {
        return NotFound();
      }

      // Get exam ID before deleting the question
      var examId = question.QuestionSet.ExamQuestionSetMappings.FirstOrDefault()?.ExamId;
      if (examId == null)
      {
        TempData["ErrorMessage"] = "لم يتم العثور على الامتحان المرتبط بهذا السؤال.";
        return RedirectToAction(nameof(Index));
      }

      try
      {
        var success = await _questionManagementService.DeleteQuestionAsync(id);
        if (success)
        {
          TempData["SuccessMessage"] = "تم حذف السؤال بنجاح.";
        }
        else
        {
          TempData["ErrorMessage"] = "حدث خطأ أثناء حذف السؤال.";
        }
      }
      catch (Exception ex)
      {
        TempData["ErrorMessage"] = $"حدث خطأ أثناء حذف السؤال: {ex.Message}";
      }

      return RedirectToAction(nameof(ExamQuestions), new { id = examId });
    }

    // GET: Exams/AddQuestion/5
    public async Task<IActionResult> AddQuestion(int id)
    {
      var questionSets = await _questionSetService.GetQuestionSetsByExamAsync(id);

      if (!questionSets.Any())
      {
        TempData["ErrorMessage"] = "لا توجد مجموعات أسئلة متاحة لهذا الامتحان. يرجى إضافة مجموعة أسئلة أولاً.";
        return RedirectToAction(nameof(Details), new { id });
      }

      ViewBag.QuestionSets = new SelectList(questionSets, "Id", "Name");
      ViewBag.ExamId = id;
      return View(new AddQuestionViewModel { ExamId = id });
    }

    // POST: Exams/AddQuestion/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddQuestion(int id, AddQuestionViewModel model)
    {
      if (id != model.ExamId)
      {
        return NotFound();
      }

      if (ModelState.IsValid)
      {
        try
        {
          var question = await _questionManagementService.CreateQuestionAsync(id, model);
          if (question != null)
          {
            TempData["SuccessMessage"] = "تم إضافة السؤال بنجاح.";
            return RedirectToAction(nameof(ExamQuestions), new { id });
          }

          ModelState.AddModelError("", "حدث خطأ أثناء إضافة السؤال.");
        }
        catch (Exception ex)
        {
          ModelState.AddModelError("", $"حدث خطأ أثناء إضافة السؤال: {ex.Message}");
        }
      }

      // Reload data in case of error
      var questionSets = await _questionSetService.GetQuestionSetsByExamAsync(id);
      ViewBag.QuestionSets = new SelectList(questionSets, "Id", "Name");
      ViewBag.ExamId = id;
      return View(model);
    }

    #region Private Helper Methods

    private bool ExamExists(int id)
    {
      return _context.Exams.Any(e => e.Id == id);
    }

    #endregion
  }
}
