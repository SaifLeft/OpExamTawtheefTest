using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using TawtheefTest.ViewModels;
using TawtheefTest.DTOs;
using TawtheefTest.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using TawtheefTest.Enum;
using Microsoft.AspNetCore.Http;
using System.IO;
using AutoMapper;
using ITAM.Service;
using TawtheefTest.DTOs.Common;

namespace TawtheefTest.Controllers
{
  public class QuestionSetsController : Controller
  {
    private readonly ApplicationDbContext _context;
    private readonly IOpExamsService _opExamsService;
    private readonly IOpExamQuestionGenerationService _questionGenerationService;
    private readonly IMapper _mapper;
    private readonly IQuestionSetLibraryService _questionSetLibraryService;
    private readonly IFileMangmanent _file;

    public QuestionSetsController(
        ApplicationDbContext context,
        IOpExamsService opExamsService,
        IOpExamQuestionGenerationService questionGenerationService,
        IMapper mapper,
        IQuestionSetLibraryService questionSetLibraryService,
        IFileMangmanent file)
    {
      _context = context;
      _opExamsService = opExamsService;
      _questionGenerationService = questionGenerationService;
      _mapper = mapper;
      _questionSetLibraryService = questionSetLibraryService;
      _file = file;
    }

    // GET: QuestionSets
    public async Task<IActionResult> Index()
    {
      var questionSets = await _context.QuestionSets
          .Include(qs => qs.ExamQuestionSets)
              .ThenInclude(eqs => eqs.Exam)
          .OrderByDescending(qs => qs.CreatedAt)
          .ToListAsync();

      var questionSetDtos = _mapper.Map<List<QuestionSetDto>>(questionSets);
      return View(questionSetDtos);
    }

    // GET: QuestionSets/Details/5
    public async Task<IActionResult> Details(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var questionSetDto = await _questionGenerationService.GetQuestionSetDetailsAsync(id.Value);
      if (questionSetDto == null)
      {
        return NotFound();
      }

      return View(questionSetDto);
    }

    // GET: QuestionSets/Status/5
    public async Task<IActionResult> Status(int id)
    {
      var questionSetDto = await _questionGenerationService.GetQuestionSetStatusAsync(id);
      if (questionSetDto == null)
      {
        return NotFound();
      }

      var viewModel = _mapper.Map<QuestionSetStatusViewModel>(questionSetDto);
      return PartialView("_StatusPartial", viewModel);
    }

    // GET: QuestionSets/Create
    public IActionResult Create(int? examId)
    {
      var viewModel = new CreateQuestionSetViewModel
      {
        ExamId = examId ?? 0,
        QuestionCount = 10,
        Difficulty = "auto",
        Language = "Arabic"
      };

      ViewBag.QuestionTypes = GetQuestionTypes();
      ViewBag.ContentSourceTypes = GetContentSourceTypes();
      ViewBag.DifficultyLevels = GetDifficultyLevels();

      return View(viewModel);
    }

    // POST: QuestionSets/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateQuestionSetViewModel model)
    {
      if (ModelState.IsValid)
      {
        // Create a new question set with failed status
        var questionSet = new QuestionSet
        {
          Name = model.Name,
          Description = model.Description,
          QuestionType = model.QuestionType,
          Difficulty = model.Difficulty,
          QuestionCount = model.QuestionCount,
          Language = model.Language,
          OptionsCount = model.OptionsCount,
          NumberOfRows = model.NumberOfRows,
          NumberOfCorrectOptions = model.NumberOfCorrectOptions,
          Status = QuestionSetStatus.Failed,
          CreatedAt = DateTime.UtcNow,
        };

        _context.Add(questionSet);
        await _context.SaveChangesAsync();

        var contentSource = new ContentSource
        {
          QuestionSetId = questionSet.Id,
          ContentSourceType = model.ContentSourceType,
          Content = model.TextContent,
          CreatedAt = DateTime.UtcNow
        };

        if (model.File != null && model.File.Length > 0)
        {
          ContentSourceType type = System.Enum.Parse<ContentSourceType>(model.ContentSourceType, true);
          FileRespoesDTO respoesDTO = _file.SaveFile(model.File, type);
          if (respoesDTO == null && !respoesDTO.IsSuccess)
          {
            throw new Exception(respoesDTO.Message);
          }
          contentSource.UploadedFile = new UploadedFile
          {
            FileName = respoesDTO.FileName,
            FileId = respoesDTO.Path,
            FileType = respoesDTO.FileExtension,
            ContentType = model.File.ContentType,
            FileSize = model.File.Length,
            CreatedAt = DateTime.UtcNow,
          };
        }

        _context.ContentSources.Add(contentSource);
        await _context.SaveChangesAsync();

        if (model.ExamId > 0)
        {
          var examQuestionSet = new ExamQuestionSet
          {
            ExamId = model.ExamId,
            QuestionSetId = questionSet.Id,
            DisplayOrder = 1
          };

          _context.ExamQuestionSets.Add(examQuestionSet);
          await _context.SaveChangesAsync();
        }

        //await _opExamsService.GenerateQuestionsAsync(questionSet.Id);

        TempData["SuccessMessage"] = "تم إنشاء مجموعة الأسئلة بنجاح وبدء عملية توليد الأسئلة.";

        if (model.ExamId > 0)
        {
          return RedirectToAction("Details", "Exams", new { id = model.ExamId });
        }
        else
        {
          return RedirectToAction(nameof(Index));
        }
      }

      ViewBag.QuestionTypes = GetQuestionTypes();
      ViewBag.ContentSourceTypes = GetContentSourceTypes();
      ViewBag.DifficultyLevels = GetDifficultyLevels();

      return View(model);
    }

    // GET: QuestionSets/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var questionSet = await _context.QuestionSets
          .Include(qs => qs.ContentSources)
          .Include(qs => qs.ExamQuestionSets)
          .FirstOrDefaultAsync(qs => qs.Id == id);

      if (questionSet == null)
      {
        return NotFound();
      }

      if (questionSet.Status == QuestionSetStatus.Processing)
      {
        TempData["ErrorMessage"] = "لا يمكن تعديل مجموعة الأسئلة لأنها قيد المعالجة حاليًا.";
        return RedirectToAction(nameof(Details), new { id });
      }

      var model = new EditQuestionSetViewModel
      {
        Id = questionSet.Id,
        Name = questionSet.Name,
        Description = questionSet.Description,
        QuestionType = questionSet.QuestionType,
        Language = questionSet.Language,
        Difficulty = questionSet.Difficulty,
        QuestionCount = questionSet.QuestionCount,
        OptionsCount = questionSet.OptionsCount ?? 4,
        NumberOfRows = questionSet.NumberOfRows,
        NumberOfCorrectOptions = questionSet.NumberOfCorrectOptions,
        IsProcessing = questionSet.Status == QuestionSetStatus.Processing
      };

      var examQuestionSet = questionSet.ExamQuestionSets?.FirstOrDefault();
      if (examQuestionSet != null)
      {
        model.ExamId = examQuestionSet.ExamId;
      }

      var contentSource = questionSet.ContentSources?.FirstOrDefault();
      if (contentSource != null)
      {
        string contentType = contentSource.ContentSourceType?.ToLower() ?? "";
        model.CurrentContentType = contentType;

        switch (contentType)
        {
          case "topic":
            model.Topic = contentSource.Content;
            break;
          case "text":
            model.TextContent = contentSource.Content;
            break;
          case "link":
            model.LinkUrl = contentSource.Url;
            break;
          case "youtube":
            model.YoutubeUrl = contentSource.Url;
            break;
          default:
            if (new[] { "document", "image", "audio", "video" }.Contains(contentType))
            {
              model.FileReference = contentSource.UploadedFileId?.ToString();
              model.CurrentFileReference = contentSource.UploadedFileId?.ToString();
            }
            break;
        }
      }

      ViewBag.QuestionSetId = questionSet.Id;
      ViewBag.Exams = new SelectList(_context.Exams.ToList(), "Id", "Name");
      return View(model);
    }

    // POST: QuestionSets/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditQuestionSetViewModel model)
    {
      var questionSet = await _context.QuestionSets
          .Include(qs => qs.ContentSources)
          .Include(qs => qs.ExamQuestionSets)
          .FirstOrDefaultAsync(qs => qs.Id == id);

      if (questionSet == null)
      {
        return NotFound();
      }

      if (questionSet.Status == QuestionSetStatus.Processing)
      {
        TempData["ErrorMessage"] = "لا يمكن تعديل مجموعة الأسئلة لأنها قيد المعالجة حاليًا.";
        return RedirectToAction(nameof(Details), new { id });
      }

      if (ModelState.IsValid)
      {
        try
        {
          questionSet.Name = model.Name;
          questionSet.Description = model.Description;
          questionSet.QuestionType = model.QuestionType;
          questionSet.Language = model.Language;
          questionSet.Difficulty = model.Difficulty;
          questionSet.QuestionCount = model.QuestionCount;
          questionSet.OptionsCount = model.OptionsCount;
          questionSet.NumberOfRows = model.NumberOfRows;
          questionSet.NumberOfCorrectOptions = model.NumberOfCorrectOptions;
          questionSet.UpdatedAt = DateTime.UtcNow;

          if (questionSet.Status == QuestionSetStatus.Completed || questionSet.Status == QuestionSetStatus.Failed)
          {
            questionSet.Status = QuestionSetStatus.Pending;
            questionSet.ProcessedAt = null;
            questionSet.ErrorMessage = null;

            var questions = await _context.Questions
                .Where(q => q.QuestionSetId == id)
                .ToListAsync();

            _context.Questions.RemoveRange(questions);
          }

          _context.Update(questionSet);

          if (model.ExamId > 0)
          {
            var examQuestionSet = await _context.ExamQuestionSets
                .FirstOrDefaultAsync(eqs => eqs.QuestionSetId == id);

            if (examQuestionSet != null)
            {
              if (examQuestionSet.ExamId != model.ExamId)
              {
                examQuestionSet.ExamId = model.ExamId;
                _context.Update(examQuestionSet);
              }
            }
            else
            {
              var newExamQuestionSet = new ExamQuestionSet
              {
                ExamId = model.ExamId,
                QuestionSetId = id,
                DisplayOrder = 1
              };
              _context.Add(newExamQuestionSet);
            }
          }

          if (model.File != null && model.File.Length > 0)
          {
            using var stream = model.File.OpenReadStream();
            var fileId = await _opExamsService.UploadFile(stream, model.File.FileName);
            if (!string.IsNullOrEmpty(fileId))
            {
              var file = new UploadedFile
              {
                FileName = model.File.FileName,
                FileId = fileId,
                FileType = DetermineFileType(model.File.ContentType),
                ContentType = model.File.ContentType,
                FileSize = model.File.Length,
                CreatedAt = DateTime.UtcNow
              };

              _context.UploadedFiles.Add(file);
              await _context.SaveChangesAsync();

              var contentSource = questionSet.ContentSources.FirstOrDefault();
              string contentType = DetermineContentType(model.File.ContentType);

              if (contentSource != null)
              {
                contentSource.ContentSourceType = contentType;
                contentSource.UploadedFileId = file.Id;
                contentSource.Content = null;
                contentSource.Url = null;
                _context.Update(contentSource);
              }
              else
              {
                contentSource = new ContentSource
                {
                  ContentSourceType = contentType,
                  UploadedFileId = file.Id,
                  QuestionSetId = id
                };
                _context.Add(contentSource);
              }
            }
          }
          else
          {
            var contentSource = questionSet.ContentSources.FirstOrDefault();

            if (!string.IsNullOrEmpty(model.Topic))
            {
              UpdateContentSource(contentSource, ContentSourceType.Topic.ToString(), model.Topic, null, null, id);
            }
            else if (!string.IsNullOrEmpty(model.TextContent))
            {
              UpdateContentSource(contentSource, ContentSourceType.Text.ToString(), model.TextContent, null, null, id);
            }
            else if (!string.IsNullOrEmpty(model.LinkUrl))
            {
              UpdateContentSource(contentSource, ContentSourceType.Link.ToString(), null, model.LinkUrl, null, id);
            }
            else if (!string.IsNullOrEmpty(model.YoutubeUrl))
            {
              UpdateContentSource(contentSource, ContentSourceType.Youtube.ToString(), null, model.YoutubeUrl, null, id);
            }
          }

          await _context.SaveChangesAsync();

          TempData["SuccessMessage"] = "تم تحديث مجموعة الأسئلة بنجاح وإعادة تقديمها لتوليد الأسئلة.";
          return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateConcurrencyException)
        {
          if (!QuestionSetExists(id))
          {
            return NotFound();
          }
          else
          {
            throw;
          }
        }
      }

      ViewBag.Exams = new SelectList(_context.Exams.ToList(), "Id", "Name", model.ExamId);
      return View(model);
    }

    private void UpdateContentSource(ContentSource contentSource, string contentType, string content, string url, int? fileId, int questionSetId)
    {
      if (contentSource != null)
      {
        contentSource.ContentSourceType = contentType;
        contentSource.Content = content;
        contentSource.Url = url;
        contentSource.UploadedFileId = fileId;
        _context.Update(contentSource);
      }
      else
      {
        contentSource = new ContentSource
        {
          ContentSourceType = contentType,
          Content = content,
          Url = url,
          UploadedFileId = fileId,
          QuestionSetId = questionSetId
        };
        _context.Add(contentSource);
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
          .Include(qs => qs.ExamQuestionSets)
              .ThenInclude(eqs => eqs.Exam)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (questionSet == null)
      {
        return NotFound();
      }



      // لا نسمح بحذف مجموعة الأسئلة التي قيد المعالجة
      if (questionSet.Status == QuestionSetStatus.Processing)
      {
        TempData["ErrorMessage"] = "لا يمكن حذف مجموعة الأسئلة لأنها قيد المعالجة حاليًا.";
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
          .Include(qs => qs.Questions)
          .Include(qs => qs.ExamQuestionSets)
          .Include(qs => qs.ContentSources)
          .FirstOrDefaultAsync(qs => qs.Id == id);

      if (questionSet != null)
      {
        // لا نسمح بحذف مجموعة الأسئلة التي قيد المعالجة
        if (questionSet.Status == QuestionSetStatus.Processing)
        {
          TempData["ErrorMessage"] = "لا يمكن حذف مجموعة الأسئلة لأنها قيد المعالجة حاليًا.";
          return RedirectToAction(nameof(Details), new { id });
        }

        _context.QuestionSets.Remove(questionSet);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "تم حذف مجموعة الأسئلة بنجاح";
      }

      return RedirectToAction(nameof(Index));
    }

    // POST: QuestionSets/Retry/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Retry(int id)
    {
      var result = await _questionGenerationService.RetryQuestionGenerationAsync(id);
      if (!result)
      {
        TempData["ErrorMessage"] = "تعذر إعادة محاولة توليد الأسئلة. يجب أن تكون المجموعة في حالة فشل.";
        return RedirectToAction("Details", new { id });
      }

      TempData["SuccessMessage"] = "تم إعادة محاولة توليد الأسئلة بنجاح.";
      return RedirectToAction("Details", new { id });
    }

    // GET: QuestionSets/AddToExam/5
    public async Task<IActionResult> AddToExam(int id)
    {
      var questionSet = await _questionGenerationService.GetQuestionSetDetailsAsync(id);
      if (questionSet == null)
      {
        return NotFound();
      }

      var exams = await _context.Exams
          .Where(e => e.Status != ExamStatus.Archived)
          .Select(e => new SelectListItem
          {
            Value = e.Id.ToString(),
            Text = e.Name
          }).ToListAsync();

      ViewBag.Exams = exams;
      ViewBag.QuestionSet = questionSet;

      return View(new AddQuestionSetToExamViewModel { QuestionSetId = id });
    }

    // POST: QuestionSets/AddToExam
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToExam(AddQuestionSetToExamViewModel model)
    {
      if (ModelState.IsValid)
      {
        // استخدام الخدمة لإضافة مجموعة الأسئلة إلى الاختبار إذا كانت متاحة
        if (_questionSetLibraryService != null)
        {
          await _questionSetLibraryService.AddQuestionSetToExamAsync(model.ExamId, model.QuestionSetId, model.DisplayOrder);
        }
        else
        {
          // إذا لم تكن الخدمة متاحة، استخدم الطريقة التقليدية
          var examQuestionSet = new ExamQuestionSet
          {
            ExamId = model.ExamId,
            QuestionSetId = model.QuestionSetId,
            DisplayOrder = model.DisplayOrder
          };

          _context.ExamQuestionSets.Add(examQuestionSet);
          await _context.SaveChangesAsync();
        }

        return RedirectToAction("Details", "Exams", new { id = model.ExamId });
      }

      var questionSet = await _questionGenerationService.GetQuestionSetDetailsAsync(model.QuestionSetId);
      var exams = await _context.Exams
          .Where(e => e.Status != ExamStatus.Archived)
          .Select(e => new SelectListItem
          {
            Value = e.Id.ToString(),
            Text = e.Name
          }).ToListAsync();

      ViewBag.Exams = exams;
      ViewBag.QuestionSet = questionSet;

      return View(model);
    }

    // POST: QuestionSets/GenerateAgain/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateAgain(int id)
    {
      var questionSet = await _context.QuestionSets
          .Include(qs => qs.Questions)
          .FirstOrDefaultAsync(qs => qs.Id == id);

      if (questionSet == null)
      {
        return NotFound();
      }

      // حذف الأسئلة الموجودة
      _context.Questions.RemoveRange(questionSet.Questions);
      await _context.SaveChangesAsync();

      // إعادة تعيين حالة مجموعة الأسئلة
      questionSet.Status = QuestionSetStatus.Pending;
      questionSet.ErrorMessage = null;
      await _context.SaveChangesAsync();

      // بدء عملية إنشاء الأسئلة مرة أخرى
      await _opExamsService.GenerateQuestionsAsync(questionSet.Id);

      TempData["SuccessMessage"] = "تم بدء عملية توليد الأسئلة مرة أخرى.";
      return RedirectToAction(nameof(Details), new { id });
    }

    private bool QuestionSetExists(int id)
    {
      return _context.QuestionSets.Any(e => e.Id == id);
    }

    private string DetermineFileType(string contentType)
    {
      if (contentType.StartsWith("image/"))
      {
        return nameof(FileType.Image);
      }
      else if (contentType.StartsWith("audio/"))
      {
        return nameof(FileType.Audio);
      }
      else if (contentType.StartsWith("video/"))
      {
        return nameof(FileType.Video);
      }
      else if (contentType == "application/pdf" ||
              contentType == "application/msword" ||
              contentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document" ||
              contentType == "text/plain")
      {
        return nameof(FileType.Document);
      }
      else
      {
        return nameof(FileType.Other);
      }
    }

    private string DetermineContentType(string contentType)
    {
      if (contentType.StartsWith("image/"))
      {
        return ContentSourceType.Image.ToString();
      }
      else if (contentType.StartsWith("audio/"))
      {
        return ContentSourceType.Audio.ToString();
      }
      else if (contentType.StartsWith("video/"))
      {
        return ContentSourceType.Video.ToString();
      }
      else
      {
        return ContentSourceType.Document.ToString();
      }
    }

    // Helpers for dropdown lists
    private IEnumerable<SelectListItem> GetQuestionTypes()
    {
      return new List<SelectListItem>
      {
        new SelectListItem { Value = "MCQ", Text = "اختيار من متعدد" },
        new SelectListItem { Value = "TF", Text = "صح / خطأ" },
        new SelectListItem { Value = "open", Text = "إجابة مفتوحة" },
        new SelectListItem { Value = "fillInTheBlank", Text = "ملء الفراغات" },
        new SelectListItem { Value = "ordering", Text = "ترتيب" },
        new SelectListItem { Value = "matching", Text = "مطابقة" },
        new SelectListItem { Value = "multiSelect", Text = "اختيار متعدد" },
        new SelectListItem { Value = "shortAnswer", Text = "إجابة قصيرة" }
      };
    }

    private IEnumerable<SelectListItem> GetContentSourceTypes()
    {
      return new List<SelectListItem>
      {
        new SelectListItem { Value = "topic", Text = "موضوع" },
        new SelectListItem { Value = "text", Text = "نص" },
        new SelectListItem { Value = "link", Text = "رابط" },
        new SelectListItem { Value = "youtube", Text = "يوتيوب" },
        new SelectListItem { Value = "document", Text = "مستند" },
        new SelectListItem { Value = "image", Text = "صورة" },
        new SelectListItem { Value = "audio", Text = "صوت" },
        new SelectListItem { Value = "video", Text = "فيديو" }
      };
    }

    private IEnumerable<SelectListItem> GetDifficultyLevels()
    {
      return new List<SelectListItem>
      {
        new SelectListItem { Value = "easy", Text = "سهل" },
        new SelectListItem { Value = "medium", Text = "متوسط" },
        new SelectListItem { Value = "hard", Text = "صعب" },
        new SelectListItem { Value = "auto", Text = "تلقائي" }
      };
    }

    // تم نقلها من QuestionSetsLibraryController
    // GET: QuestionSets/Library
    public async Task<IActionResult> Library()
    {
      var questionSets = await _questionSetLibraryService.GetAllQuestionSetsAsync();
      return View(questionSets);
    }

    // GET: QuestionSets/LibraryDetails/5
    public async Task<IActionResult> LibraryDetails(int id)
    {
      var questionSet = await _questionSetLibraryService.GetQuestionSetDetailsAsync(id);
      if (questionSet == null)
      {
        return NotFound();
      }

      return View(questionSet);
    }

    // POST: QuestionSets/Clone/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clone(int id)
    {
      var newQuestionSetId = await _questionSetLibraryService.CloneQuestionSetAsync(id);
      if (newQuestionSetId == 0)
      {
        return NotFound();
      }

      TempData["SuccessMessage"] = "تم نسخ مجموعة الأسئلة بنجاح";
      return RedirectToAction(nameof(LibraryDetails), new { id = newQuestionSetId });
    }

    // GET: QuestionSets/ShuffleOptions/5
    public async Task<IActionResult> ShuffleOptions(int id)
    {
      var questionSet = await _questionSetLibraryService.GetQuestionSetDetailsAsync(id);
      if (questionSet == null)
      {
        return NotFound();
      }

      ViewBag.QuestionSet = questionSet;
      return View(new ShuffleOptionsViewModel { QuestionSetId = id });
    }

    // POST: QuestionSets/ShuffleOptions
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ShuffleOptions(ShuffleOptionsViewModel model)
    {
      if (ModelState.IsValid)
      {
        await _questionSetLibraryService.ShuffleQuestionOptionsAsync(model.QuestionSetId, model.ShuffleType);
        TempData["SuccessMessage"] = "تم خلط خيارات الأسئلة بنجاح";
        return RedirectToAction(nameof(LibraryDetails), new { id = model.QuestionSetId });
      }

      var questionSet = await _questionSetLibraryService.GetQuestionSetDetailsAsync(model.QuestionSetId);
      ViewBag.QuestionSet = questionSet;
      return View(model);
    }

    // GET: QuestionSets/DownloadDocument/5
    public async Task<IActionResult> DownloadDocument(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var questionSet = await _context.QuestionSets
          .Include(q => q.ContentSources)
              .ThenInclude(cs => cs.UploadedFile)
          .FirstOrDefaultAsync(q => q.Id == id);

      if (questionSet == null || questionSet.ContentSources == null || !questionSet.ContentSources.Any())
      {
        return NotFound();
      }

      var contentSource = questionSet.ContentSources.FirstOrDefault();
      if (contentSource == null ||
          contentSource.ContentSourceType != ContentSourceType.Document.ToString() ||
          contentSource.UploadedFile == null)
      {
        return BadRequest("لا يوجد مستند مرفق لهذه المجموعة");
      }

      var fileResponse = _file.GetFileByName(contentSource.UploadedFile.FileName, ContentSourceType.Document);
      if (fileResponse == null || !fileResponse.IsSuccess)
      {
        return NotFound("لم يتم العثور على الملف المطلوب");
      }

      return File(fileResponse.FileBytes, "application/octet-stream", contentSource.UploadedFile.FileName);
    }
  }
}
