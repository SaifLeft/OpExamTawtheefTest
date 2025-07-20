using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ITAM.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using TawtheefTest.DTOs;
using TawtheefTest.DTOs.Common;
using TawtheefTest.Enums;
using TawtheefTest.Services;
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
    public QuestionSetsController(
        ApplicationDbContext context,
        IMapper mapper,
        IQuestionSetLibraryService libraryService,
        IQuestionGenerationService questionGenerationService,
        IFileMangmanent file)
    {
      _context = context;
      _mapper = mapper;
      _libraryService = libraryService;
      _questionGenerationService = questionGenerationService;
      _file = file;
    }

    // GET: QuestionSets
    public async Task<IActionResult> Index(string search = null, string questionType = null, string difficulty = null, string language = null)
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
      ViewBag.QuestionTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "MCQ", Text = "اختيار من متعدد" },
                new SelectListItem { Value = "TF", Text = "صح / خطأ" },
                new SelectListItem { Value = "open", Text = "إجابة مفتوحة" },
                new SelectListItem { Value = "fillInTheBlank", Text = "ملء الفراغات" },
                new SelectListItem { Value = "ordering", Text = "ترتيب" },
                new SelectListItem { Value = "matching", Text = "مطابقة" },
                new SelectListItem { Value = "multiSelect", Text = "اختيار متعدد (أكثر من إجابة)" },
                new SelectListItem { Value = "shortAnswer", Text = "إجابة قصيرة" }
            };

      ViewBag.DifficultyLevels = new List<SelectListItem>
            {
                new SelectListItem { Value = "auto", Text = "تلقائي" },
                new SelectListItem { Value = "easy", Text = "سهل" },
                new SelectListItem { Value = "medium", Text = "متوسط" },
                new SelectListItem { Value = "hard", Text = "صعب" }
            };

      ViewBag.ContentSourceTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "text", Text = "نص" },
                new SelectListItem { Value = "topic", Text = "موضوع" },
                new SelectListItem { Value = "link", Text = "رابط" },
                new SelectListItem { Value = "youtube", Text = "فيديو يوتيوب" },
                new SelectListItem { Value = "document", Text = "مستند (PDF/Word)" },
                new SelectListItem { Value = "image", Text = "صورة" },
                new SelectListItem { Value = "audio", Text = "ملف صوتي" },
                new SelectListItem { Value = "video", Text = "فيديو" }
            };

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
            ContentSourceType contentSourceTypeEnum = (ContentSourceType)System.Enum.Parse(typeof(ContentSourceType), model.ContentSourceType, true);
            var SaveResponse = _file.SaveFile(model.File, contentSourceTypeEnum);
            if (SaveResponse.IsSuccess)
            {
              createDto.FileName = SaveResponse.FileName;
            }
            else
            {
              ModelState.AddModelError("", "حدث خطأ أثناء حفظ الملف");
              PrepareViewBags();
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
            PrepareViewBags();
            return View(model);
          }
        }
        catch (Exception ex)
        {
          ModelState.AddModelError("", $"حدث خطأ: {ex.Message}");
          PrepareViewBags();
          return View(model);
        }
      }

      PrepareViewBags();
      return View(model);

      void PrepareViewBags()
      {
        ViewBag.QuestionTypes = new List<SelectListItem>
                {
                    new SelectListItem { Value = "MCQ", Text = "اختيار من متعدد" },
                    new SelectListItem { Value = "TF", Text = "صح / خطأ" },
                    new SelectListItem { Value = "open", Text = "إجابة مفتوحة" },
                    new SelectListItem { Value = "fillInTheBlank", Text = "ملء الفراغات" },
                    new SelectListItem { Value = "ordering", Text = "ترتيب" },
                    new SelectListItem { Value = "matching", Text = "مطابقة" },
                    new SelectListItem { Value = "multiSelect", Text = "اختيار متعدد (أكثر من إجابة)" },
                    new SelectListItem { Value = "shortAnswer", Text = "إجابة قصيرة" }
                };

        ViewBag.DifficultyLevels = new List<SelectListItem>
                {
                    new SelectListItem { Value = "auto", Text = "تلقائي" },
                    new SelectListItem { Value = "easy", Text = "سهل" },
                    new SelectListItem { Value = "medium", Text = "متوسط" },
                    new SelectListItem { Value = "hard", Text = "صعب" }
                };

        ViewBag.ContentSourceTypes = new List<SelectListItem>
                {
                    new SelectListItem { Value = "text", Text = "نص" },
                    new SelectListItem { Value = "topic", Text = "موضوع" },
                    new SelectListItem { Value = "link", Text = "رابط" },
                    new SelectListItem { Value = "youtube", Text = "فيديو يوتيوب" },
                    new SelectListItem { Value = "document", Text = "مستند (PDF/Word)" },
                    new SelectListItem { Value = "image", Text = "صورة" },
                    new SelectListItem { Value = "audio", Text = "ملف صوتي" },
                    new SelectListItem { Value = "video", Text = "فيديو" }
                };
      }
    }



    // GET: QuestionSets/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var questionSet = await _context.QuestionSets
          .Include(q => q.ExamQuestionSetManppings)
          .FirstOrDefaultAsync(q => q.Id == id);

      if (questionSet == null)
      {
        return NotFound();
      }

      if (questionSet.ExamQuestionSetManppings.Any())
      {
        TempData["ErrorMessage"] = "لا يمكن حذف المجموعة لأنها مستخدمة في اختبارات";
        return RedirectToAction(nameof(Details), new { id });
      }

      QuestionSetDto questionSetDto = _mapper.Map<QuestionSetDto>(questionSet);

      return View(questionSetDto);
    }

    // POST: QuestionSets/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      var questionSet = await _context.QuestionSets
          .Include(q => q.ExamQuestionSetManppings)
          .FirstOrDefaultAsync(q => q.Id == id);

      if (questionSet == null)
      {
        return NotFound();
      }

      if (questionSet.ExamQuestionSetManppings.Any())
      {
        TempData["ErrorMessage"] = "لا يمكن حذف المجموعة لأنها مستخدمة في اختبارات";
        return RedirectToAction(nameof(Index));
      }

      _context.QuestionSets.Remove(questionSet);
      await _context.SaveChangesAsync();

      TempData["SuccessMessage"] = "تم حذف المجموعة بنجاح";
      return RedirectToAction(nameof(Index));
    }

    // GET: QuestionSets/Status/5
    [HttpGet]
    public async Task<IActionResult> Status(int id)
    {
      var questionSet = await _context.QuestionSets.FindAsync(id);
      if (questionSet == null)
      {
        return NotFound();
      }

      return Json(new { status = questionSet.Status.ToString() });
    }

    // GET: QuestionSets/ShuffleOptions/5
    [HttpGet]
    public async Task<IActionResult> ShuffleOptions(int id, string shuffleType)
    {
      try
      {
        ShuffleType type = (ShuffleType)System.Enum.Parse(typeof(ShuffleType), shuffleType);
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
      var questionSet = await _context.QuestionSets.FindAsync(id);
      if (questionSet == null)
      {
        return NotFound();
      }

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
      var questionSet = await _context.QuestionSets
          .Include(q => q.ExamQuestionSetManppings)
          .ThenInclude(e => e.Exam)
          .ThenInclude(e => e.Job)
          .FirstOrDefaultAsync(q => q.Id == id);

      if (questionSet == null || questionSet.Status != QuestionSetStatus.Completed)
      {
        return NotFound();
      }

      var viewModel = new AddQuestionSetToExamViewModel
      {
        QuestionSetId = questionSet.Id,
        QuestionSetName = questionSet.Name,
        QuestionType = questionSet.QuestionType,
        Language = questionSet.Language == "Arabic" ? QuestionSetLanguage.Arabic : QuestionSetLanguage.English,
        QuestionCount = questionSet.QuestionCount,
        DisplayOrder = 1 
      };

      // الاختبارات المرتبطة حالياً
      viewModel.AssignedExams = questionSet.ExamQuestionSetManppings
          .Select(e => new ExamSummaryViewModel
          {
            Id = e.Exam.Id,
            Name = e.Exam.Name,
            JobTitle = e.Exam.Job.Title,
            Status = e.Exam.Status
          })
          .ToList();

      // الاختبارات المتاحة للإضافة
      var assignedExamIds = viewModel.AssignedExams.Select(e => e.Id).ToList();
      var availableExams = await _context.Exams
          .Include(e => e.Job)
          .Where(e => !assignedExamIds.Contains(e.Id) && e.Status != ExamStatus.Archived)
          .ToListAsync();

      viewModel.AvailableExams = availableExams
          .Select(e => new ExamSummaryViewModel
          {
            Id = e.Id,
            Name = e.Name,
            JobTitle = e.Job.Title,
            Status = e.Status
          })
          .ToList();

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

      AddQuestionSetToExamViewModel model = new AddQuestionSetToExamViewModel
      {
        QuestionSetId = DTO.QuestionSetId,
        DisplayOrder = DTO.DisplayOrder
      };

      // إعادة تجهيز النموذج في حالة الخطأ
      var questionSet = await _context.QuestionSets
          .Include(q => q.ExamQuestionSetManppings)
          .ThenInclude(e => e.Exam)
          .ThenInclude(e => e.Job)
          .FirstOrDefaultAsync(q => q.Id == DTO.QuestionSetId);

      // الاختبارات المرتبطة حالياً
      model.AssignedExams = questionSet.ExamQuestionSetManppings
          .Select(e => new ExamSummaryViewModel
          {
            Id = e.Exam.Id,
            Name = e.Exam.Name,
            JobTitle = e.Exam.Job.Title,
            Status = e.Exam.Status
          })
          .ToList();

      // الاختبارات المتاحة للإضافة
      var assignedExamIds = model.AssignedExams.Select(e => e.Id).ToList();
      var availableExams = await _context.Exams
          .Include(e => e.Job)
          .Where(e => !assignedExamIds.Contains(e.Id) && e.Status != ExamStatus.Archived)
          .ToListAsync();

      model.AvailableExams = availableExams
          .Select(e => new ExamSummaryViewModel
          {
            Id = e.Id,
            Name = e.Name,
            JobTitle = e.Job.Title,
            Status = e.Status
          })
          .ToList();

      return View(model);
    }

    // POST: QuestionSets/RemoveFromExam
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFromExam(int questionSetId, int examId)
    {
      try
      {
        await _libraryService.RemoveQuestionSetFromExam(examId, questionSetId);
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
        var newId = await _libraryService.CloneQuestionSetAsync(id);
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
      var questionSet = await _context.QuestionSets
          .FirstOrDefaultAsync(qs => qs.Id == id && qs.ContentSourceType == "document");

      if (questionSet == null || string.IsNullOrEmpty(questionSet.FileUploadedCode))
      {
        return NotFound();
      }

      try
      {
        var contentSourceType = Enum.Parse<ContentSourceType>(questionSet.ContentSourceType, true);
        var fileData = _file.GetFileByName(questionSet.FileUploadedCode, contentSourceType);
        if (fileData == null || fileData.FileBytes == null || fileData.FileBytes.Length == 0)
        {
          return NotFound("لم يتم العثور على الملف");
        }

        return File(fileData.FileBytes, "application/pdf", questionSet.FileName ?? "document.pdf");
      }
      catch (Exception)
      {
        return NotFound("حدث خطأ أثناء استرداد الملف");
      }
    }

    // GET: QuestionSets/DownloadFile/5
    public async Task<IActionResult> DownloadFile(int id)
    {

      var questionSet = await _context.QuestionSets
          .FirstOrDefaultAsync(qs => qs.Id == id);

      if (questionSet == null || string.IsNullOrEmpty(questionSet.FileName))
      {
        return NotFound("الملف غير موجود");
      }

      try
      {
        var contentSourceType = Enum.Parse<ContentSourceType>(questionSet.ContentSourceType, true);
        var fileData = _file.GetFileByName(questionSet.FileName, contentSourceType);
        if (fileData == null || fileData.FileBytes == null || fileData.FileBytes.Length == 0)
        {
          return NotFound("لم يتم العثور على الملف");
        }

        return File(fileData.FileBytes, fileData.FileContentType, fileData.FileName);
      }
      catch (Exception ex)
      {
        return NotFound($"حدث خطأ أثناء استرداد الملف: {ex.Message}");
      }
    }

    [HttpPost]
    public async Task<IActionResult> Merge([FromForm] MergeQuestionSetsViewModel model)
    {
      if (model.SelectedIds == null || model.SelectedIds.Count < 2)
        return Json(new { success = false, message = "يرجى اختيار مجموعتين أو أكثر للدمج." });

      if (string.IsNullOrWhiteSpace(model.MergedName))
        return Json(new { success = false, message = "يرجى إدخال اسم المجموعة الجديدة." });

      if (string.IsNullOrWhiteSpace(model.MergedType))
        return Json(new { success = false, message = "يرجى اختيار نوع الأسئلة للمجموعة الجديدة." });

      if (string.IsNullOrWhiteSpace(model.MergedDifficulty))
        return Json(new { success = false, message = "يرجى اختيار مستوى الصعوبة للمجموعة الجديدة." });

      if (string.IsNullOrWhiteSpace(model.MergedLanguage))
        return Json(new { success = false, message = "يرجى اختيار اللغة للمجموعة الجديدة." });

      // التحقق من عدد الأسئلة المطلوبة من كل مجموعة
      if (model.QuestionsCountPerSet == null)
      {
        model.QuestionsCountPerSet = new Dictionary<int, int>();

        // إذا لم يتم تحديد عدد الأسئلة، استخدم العدد الأقصى المتاح من كل مجموعة
        var questionSets = await _context.QuestionSets
            .Where(q => model.SelectedIds.Contains(q.Id))
            .Include(q => q.Questions)
            .ToListAsync();

        foreach (var set in questionSets)
        {
          model.QuestionsCountPerSet.Add(set.Id, set.Questions.Count);
        }
      }

      // تأكد من أن المجموعات متوافقة من حيث النوع إذا تم اختيار نوع محدد
      if (model.MergedType != "auto" && model.MergedType != "mixed")
      {
        var questionTypes = await _context.QuestionSets
            .Where(q => model.SelectedIds.Contains(q.Id))
            .Select(q => q.QuestionType)
            .Distinct()
            .ToListAsync();

        if (questionTypes.Count > 1)
        {
          return Json(new
          {
            success = false,
            warning = true,
            message = "تنبيه: المجموعات المختارة تحتوي على أنواع مختلفة من الأسئلة. هل تريد المتابعة رغم ذلك؟"
          });
        }
      }

      try
      {
        // استدعاء الخدمة لتنفيذ الدمج
        var newSetId = await _libraryService.MergeSetsAsync(model);

        // حساب مجموع الأسئلة التي تم دمجها فعلياً
        int totalQuestions = 0;
        foreach (var count in model.QuestionsCountPerSet.Values)
        {
          totalQuestions += count;
        }

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
