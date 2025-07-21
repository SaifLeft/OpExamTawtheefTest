using AutoMapper;
using ITAM.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TawtheefTest.Data.Structure;
using TawtheefTest.DTOs;
using TawtheefTest.DTOs.Common;
using TawtheefTest.Enums;
using TawtheefTest.Services;
using TawtheefTest.Services.Exams;
using TawtheefTest.ViewModels;

namespace TawtheefTest.Controllers
{
  public class QuestionSetsController : Controller
  {
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IQuestionSetLibraryService _libraryService;
    private readonly IQuestionGenerationService _questionGenerationService;
    private readonly IFileMangmanent _file;
    private readonly IQuestionSetService _questionSetService;
    private readonly IViewBagPreparationService _viewBagService;

    public QuestionSetsController(
        ApplicationDbContext context,
        IMapper mapper,
        IQuestionSetLibraryService libraryService,
        IQuestionGenerationService questionGenerationService,
        IFileMangmanent file,
        IQuestionSetService questionSetService,
        IViewBagPreparationService viewBagService)
    {
      _context = context;
      _mapper = mapper;
      _libraryService = libraryService;
      _questionGenerationService = questionGenerationService;
      _file = file;
      _questionSetService = questionSetService;
      _viewBagService = viewBagService;
    }

    // GET: QuestionSets
    public async Task<IActionResult> Index(string search = "", string questionType = "", string difficulty = "", string language = "")
    {
      var questionSets = await _libraryService.SearchQuestionSetsAsync(search, questionType, difficulty, language);

      ViewBag.Search = search;
      ViewBag.QuestionType = questionType;
      ViewBag.Difficulty = difficulty;
      ViewBag.Language = language;

      return View(questionSets);
    }

    // GET: QuestionSets/Details/5
    public async Task<IActionResult> Details(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var questionSet = await _libraryService.GetQuestionSetByIdAsync(id.Value);
      if (questionSet == null)
      {
        return NotFound();
      }

      return View(questionSet);
    }

    // GET: QuestionSets/Create
    public IActionResult Create()
    {
      _viewBagService.PrepareCreateQuestionSetViewBags(this);

      var model = new CreateQuestionSetViewModel
      {
        Language = "Arabic",
        Difficulty = "auto",
        QuestionCount = 10,
        OptionsCount = 4
      };

      return View(model);
    }

    // POST: QuestionSets/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateQuestionSetViewModel model)
    {
      if (ModelState.IsValid)
      {
        try
        {
          var createDto = _mapper.Map<CreateQuestionSetDto>(model);

          if (model.File?.Length > 0)
          {
            var fileResult = await _questionSetService.SaveFileAsync(model.File, model.ContentSourceType);
            if (fileResult.Success)
            {
              createDto.FileName = fileResult.FileName;
            }
            else
            {
              ModelState.AddModelError("", fileResult.ErrorMessage);
              _viewBagService.PrepareCreateQuestionSetViewBags(this);
              return View(model);
            }
          }

          var questionSetId = await _questionGenerationService.CreateQuestionSetAsync(createDto);

          if (questionSetId > 0)
          {
            TempData["SuccessMessage"] = "تم إنشاء مجموعة الأسئلة بنجاح وبدأت عملية توليد الأسئلة.";
            return RedirectToAction(nameof(Details), new { id = questionSetId });
          }
          else
          {
            ModelState.AddModelError("", "حدث خطأ أثناء إنشاء مجموعة الأسئلة");
          }
        }
        catch (Exception ex)
        {
          ModelState.AddModelError("", $"حدث خطأ: {ex.Message}");
        }
      }

      _viewBagService.PrepareCreateQuestionSetViewBags(this);
      return View(model);
    }

    // GET: QuestionSets/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var questionSet = await _questionSetService.GetQuestionSetWithDetailsAsync(id.Value);
      if (questionSet == null)
      {
        return NotFound();
      }

      var canDelete = await _questionSetService.CanDeleteQuestionSetAsync(id.Value);
      if (!canDelete)
      {
        TempData["ErrorMessage"] = "لا يمكن حذف المجموعة لأنها مستخدمة في اختبارات";
        return RedirectToAction(nameof(Details), new { id });
      }

      var questionSetDto = _mapper.Map<QuestionSetDto>(questionSet);
      return View(questionSetDto);
    }

    // POST: QuestionSets/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      var success = await _questionSetService.DeleteQuestionSetAsync(id);
      if (success)
      {
        TempData["SuccessMessage"] = "تم حذف المجموعة بنجاح";
      }
      else
      {
        TempData["ErrorMessage"] = "لا يمكن حذف المجموعة لأنها مستخدمة في اختبارات";
      }

      return RedirectToAction(nameof(Index));
    }

    // GET: QuestionSets/Status/5
    [HttpGet]
    public async Task<IActionResult> Status(int id)
    {
      var status = await _questionSetService.GetQuestionSetStatusAsync(id);
      return Json(new { status = status.ToString() });
    }

    // GET: QuestionSets/ShuffleOptions/5
    [HttpGet]
    public async Task<IActionResult> ShuffleOptions(int id, string shuffleType)
    {
      try
      {
        ShuffleType type = (ShuffleType)Enum.Parse(typeof(ShuffleType), shuffleType);
        await _libraryService.ShuffleQuestionOptionsAsync(id, type);
        TempData["SuccessMessage"] = "تم خلط خيارات الأسئلة بنجاح";
      }
      catch (Exception ex)
      {
        TempData["ErrorMessage"] = $"حدث خطأ أثناء خلط الخيارات: {ex.Message}";
      }

      return RedirectToAction(nameof(Details), new { id });
    }

    // POST: QuestionSets/Retry/5
    [HttpPost]
    public async Task<IActionResult> Retry(int id)
    {
      try
      {
        await _questionGenerationService.RetryQuestionGenerationAsync(id);
        TempData["SuccessMessage"] = "تم إعادة محاولة توليد الأسئلة بنجاح";
      }
      catch (Exception ex)
      {
        TempData["ErrorMessage"] = $"حدث خطأ أثناء إعادة المحاولة: {ex.Message}";
      }

      return RedirectToAction(nameof(Details), new { id });
    }

    // POST: QuestionSets/GenerateAgain/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateAgain(int id)
    {
      try
      {
        bool result = await _questionGenerationService.RegenerateQuestions(id);
        if (result)
        {
          TempData["SuccessMessage"] = "تم بدء عملية إعادة توليد الأسئلة بنجاح";
        }
        else
        {
          TempData["ErrorMessage"] = "حدث خطأ أثناء إعادة توليد الأسئلة";
        }
      }
      catch (Exception ex)
      {
        TempData["ErrorMessage"] = $"حدث خطأ: {ex.Message}";
      }

      return RedirectToAction(nameof(Details), new { id });
    }

    // GET: QuestionSets/AddToExam/5
    public async Task<IActionResult> AddToExam(int id)
    {
      var viewModel = await _questionSetService.PrepareAddToExamViewModelAsync(id);
      if (viewModel == null)
      {
        return NotFound();
      }

      return View(viewModel);
    }

    // POST: QuestionSets/AddToExam
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToExam(AddToExamDTO DTO)
    {
      if (ModelState.IsValid)
      {
        try
        {
          await _libraryService.AddQuestionSetToExam(DTO);
          TempData["SuccessMessage"] = "تمت إضافة مجموعة الأسئلة إلى الاختبار بنجاح";
          return RedirectToAction("Details", "Exams", new { id = DTO.ExamId });
        }
        catch (Exception ex)
        {
          ModelState.AddModelError("", ex.Message);
        }
      }

      // Reload model in case of error
      var model = await _questionSetService.PrepareAddToExamViewModelAsync(DTO.QuestionSetId);
      if (model != null)
      {
        model.DisplayOrder = DTO.DisplayOrder;
      }

      return View(model);
    }

    // POST: QuestionSets/RemoveFromExam
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFromExam(int questionSetId, int examId)
    {
      try
      {
        await _questionSetService.RemoveQuestionSetFromExamAsync(examId, questionSetId);
        TempData["SuccessMessage"] = "تمت إزالة مجموعة الأسئلة من الاختبار بنجاح";
      }
      catch (Exception ex)
      {
        TempData["ErrorMessage"] = $"حدث خطأ: {ex.Message}";
      }

      return RedirectToAction("Details", "Exams", new { id = examId });
    }

    // POST: QuestionSets/Clone/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clone(int id)
    {
      try
      {
        var newId = await _questionSetService.CloneQuestionSetAsync(id);
        TempData["SuccessMessage"] = "تم نسخ مجموعة الأسئلة بنجاح";
        return RedirectToAction(nameof(Details), new { id = newId });
      }
      catch (Exception ex)
      {
        TempData["ErrorMessage"] = $"حدث خطأ أثناء نسخ المجموعة: {ex.Message}";
        return RedirectToAction(nameof(Details), new { id });
      }
    }

    // GET: QuestionSets/DownloadDocument/5
    public async Task<IActionResult> DownloadDocument(int id)
    {
      return await _questionSetService.DownloadFileAsync(id, _file);
    }

    // GET: QuestionSets/DownloadFile/5
    public async Task<IActionResult> DownloadFile(int id)
    {
      return await _questionSetService.DownloadFileAsync(id, _file);
    }

    [HttpPost]
    public async Task<IActionResult> Merge([FromForm] MergeQuestionSetsViewModel model)
    {
      var validationResult = await _questionSetService.ValidateQuestionSetForMergeAsync(model.SelectedIds, model);
      if (!validationResult.Success)
      {
        if (validationResult.ErrorMessage.Contains("تنبيه"))
        {
          return Json(new
          {
            success = false,
            warning = true,
            message = validationResult.ErrorMessage
          });
        }
        return Json(new { success = false, message = validationResult.ErrorMessage });
      }

      try
      {
        var newSetId = await _questionSetService.MergeQuestionSetsAsync(model);

        // Calculate total questions merged
        int totalQuestions = model.QuestionsCountPerSet?.Values.Sum() ?? 0;

        return Json(new
        {
          success = true,
          redirectUrl = Url.Action("Details", new { id = newSetId }),
          message = $"تم دمج المجموعات بنجاح وإنشاء مجموعة جديدة باسم '{model.MergedName}' تحتوي على {totalQuestions} سؤال."
        });
      }
      catch (Exception ex)
      {
        return Json(new { success = false, message = "حدث خطأ أثناء دمج المجموعات: " + ex.Message });
      }
    }

    private bool QuestionSetExists(int id)
    {
      return _context.QuestionSets.Any(e => e.Id == id);
    }
  }
}
