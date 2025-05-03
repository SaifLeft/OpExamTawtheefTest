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

namespace TawtheefTest.Controllers
{
  public class QuestionSetsController : Controller
  {
    private readonly ApplicationDbContext _context;
    private readonly IOpExamsService _opExamsService;
    private readonly IQuestionGenerationService _questionGenerationService;
    private readonly IMapper _mapper;

    public QuestionSetsController(
        ApplicationDbContext context,
        IOpExamsService opExamsService,
        IQuestionGenerationService questionGenerationService,
        IMapper mapper)
    {
      _context = context;
      _opExamsService = opExamsService;
      _questionGenerationService = questionGenerationService;
      _mapper = mapper;
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
    public async Task<IActionResult> Create(int examId = 0)
    {
      if (examId > 0)
      {
        var exam = await _context.Exams
            .Include(e => e.Job)
            .FirstOrDefaultAsync(e => e.Id == examId);

        if (exam == null)
        {
          return NotFound();
        }

        var viewModel = new CreateQuestionSetViewModel
        {
          ExamId = examId,
          Name = $"مجموعة أسئلة لاختبار {exam.Name}",
          Language = "Arabic",
          Difficulty = "Medium",
          QuestionCount = 10,
          OptionsCount = 4
        };

        ViewBag.ExamName = exam.Name;
        ViewBag.Exams = new SelectList(new[] { exam }, "Id", "Name");

        return View(viewModel);
      }
      else
      {
        ViewBag.Exams = new SelectList(_context.Exams.ToList(), "Id", "Name");
        return View(new CreateQuestionSetViewModel());
      }
    }

    // POST: QuestionSets/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateQuestionSetViewModel viewModel, IFormFile uploadedFile)
    {
      if (ModelState.IsValid)
      {
        // تحويل ViewModel إلى DTO
        var questionSetDto = _mapper.Map<CreateQuestionSetDto>(viewModel);

        // معالجة الملف المرفوع إذا كان موجودًا
        if (uploadedFile != null && uploadedFile.Length > 0)
        {
          using (var stream = uploadedFile.OpenReadStream())
          {
            var fileId = await _opExamsService.UploadFile(stream, uploadedFile.FileName);
            if (!string.IsNullOrEmpty(fileId))
            {
              // تخزين الملف في قاعدة البيانات
              var file = new UploadedFile
              {
                FileName = uploadedFile.FileName,
                FileId = fileId,
                FileType = DetermineFileType(uploadedFile.ContentType),
                ContentType = uploadedFile.ContentType,
                FileSize = uploadedFile.Length,
                CreatedAt = DateTime.UtcNow
              };

              _context.UploadedFiles.Add(file);
              await _context.SaveChangesAsync();

              // تحديث معرف الملف في DTO
              questionSetDto.UploadedFileId = file.Id;
              questionSetDto.FileReference = file.Id.ToString();
            }
          }
        }

        // إنشاء مجموعة الأسئلة
        var questionSetId = await _questionGenerationService.CreateQuestionSetAsync(questionSetDto);

        TempData["SuccessMessage"] = "تم بدء عملية توليد الأسئلة بنجاح.";
        return RedirectToAction("Details", new { id = questionSetId });
      }

      ViewBag.Exams = new SelectList(_context.Exams.ToList(), "Id", "Name", viewModel.ExamId);
      return View(viewModel);
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

      // في المراحل "قيد المعالجة" أو "مكتمل"، لا نسمح بالتعديل
      if (questionSet.Status == QuestionSetStatus.Processing)
      {
        TempData["ErrorMessage"] = "لا يمكن تعديل مجموعة الأسئلة لأنها قيد المعالجة حاليًا.";
        return RedirectToAction(nameof(Details), new { id });
      }

      var model = new CreateQuestionSetViewModel
      {
        Name = questionSet.Name,
        Description = questionSet.Description,
        QuestionType = System.Enum.TryParse<QuestionTypeEnum>(questionSet.QuestionType, out var questionType) ? questionType : QuestionTypeEnum.MCQ,
        Language = questionSet.Language,
        Difficulty = questionSet.Difficulty,
        QuestionCount = questionSet.QuestionCount,
        OptionsCount = questionSet.OptionsCount ?? 4,
        NumberOfRows = questionSet.NumberOfRows,
        NumberOfCorrectOptions = questionSet.NumberOfCorrectOptions
      };

      // استرجاع الاختبار المرتبط بمجموعة الأسئلة إذا وجد
      var examQuestionSet = questionSet.ExamQuestionSets?.FirstOrDefault();
      if (examQuestionSet != null)
      {
        model.ExamId = examQuestionSet.ExamId;
      }

      // استرجاع مصدر المحتوى إذا وجد
      var contentSource = questionSet.ContentSources?.FirstOrDefault();
      if (contentSource != null)
      {
        string contentType = contentSource.ContentSourceType?.ToLower() ?? "";
        if (contentType == "topic")
        {
          model.Topic = contentSource.Content;
        }
        else if (contentType == "text")
        {
          model.TextContent = contentSource.Content;
        }
        else if (contentType == "link")
        {
          model.LinkUrl = contentSource.Url;
        }
        else if (contentType == "youtube")
        {
          model.YoutubeUrl = contentSource.Url;
        }
        else if (new[] { "document", "image", "audio", "video" }.Contains(contentType))
        {
          // الحصول على معرف الملف من UploadedFileId ومعالجته وفقًا لذلك
          if (contentSource.UploadedFileId.HasValue)
          {
            model.FileReference = contentSource.UploadedFileId.ToString();
          }
        }

        ViewBag.CurrentContentType = contentType;
        ViewBag.CurrentFileReference = contentSource.UploadedFileId?.ToString();
      }

      ViewBag.QuestionSetId = questionSet.Id;
      ViewBag.Exams = new SelectList(_context.Exams.ToList(), "Id", "Name");
      return View(model);
    }

    // POST: QuestionSets/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CreateQuestionSetViewModel model, IFormFile uploadedFile)
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
          // تحديث خصائص QuestionSet
          questionSet.Name = model.Name;
          questionSet.Description = model.Description;
          questionSet.QuestionType = model.QuestionType.ToString();
          questionSet.Language = model.Language;
          questionSet.Difficulty = model.Difficulty;
          questionSet.QuestionCount = model.QuestionCount;
          questionSet.OptionsCount = model.OptionsCount;
          questionSet.NumberOfRows = model.NumberOfRows;
          questionSet.NumberOfCorrectOptions = model.NumberOfCorrectOptions;
          questionSet.UpdatedAt = DateTime.UtcNow;

          // إذا كانت الحالة مكتملة، أعد تعيينها إلى معلقة لإعادة توليد الأسئلة
          if (questionSet.Status == QuestionSetStatus.Completed || questionSet.Status == QuestionSetStatus.Failed)
          {
            questionSet.Status = QuestionSetStatus.Pending;
            questionSet.ProcessedAt = null;
            questionSet.ErrorMessage = null;

            // حذف الأسئلة الموجودة
            var questions = await _context.Questions
                .Where(q => q.QuestionSetId == id)
                .ToListAsync();

            _context.Questions.RemoveRange(questions);
          }

          _context.Update(questionSet);

          // تحديث الاختبار المرتبط
          if (model.ExamId > 0)
          {
            // التحقق مما إذا كان هناك ارتباط بالفعل
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
              // إضافة ارتباط جديد
              var newExamQuestionSet = new ExamQuestionSet
              {
                ExamId = model.ExamId,
                QuestionSetId = id,
                DisplayOrder = 1
              };
              _context.Add(newExamQuestionSet);
            }
          }

          // معالجة الملف المرفوع جديد إذا وجد
          if (uploadedFile != null && uploadedFile.Length > 0)
          {
            using (var stream = uploadedFile.OpenReadStream())
            {
              var fileId = await _opExamsService.UploadFile(stream, uploadedFile.FileName);
              if (!string.IsNullOrEmpty(fileId))
              {
                var file = new UploadedFile
                {
                  FileName = uploadedFile.FileName,
                  FileId = fileId,
                  FileType = DetermineFileType(uploadedFile.ContentType),
                  ContentType = uploadedFile.ContentType,
                  FileSize = uploadedFile.Length,
                  CreatedAt = DateTime.UtcNow
                };

                _context.UploadedFiles.Add(file);
                await _context.SaveChangesAsync();

                // تحديث أو إنشاء ContentSource
                var contentSource = questionSet.ContentSources.FirstOrDefault();
                string contentType = DetermineContentType(uploadedFile.ContentType);

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
          }
          else
          {
            // تحديث مصدر المحتوى بناءً على المدخلات الأخرى
            var contentSource = questionSet.ContentSources.FirstOrDefault();

            if (!string.IsNullOrEmpty(model.Topic))
            {
              if (contentSource != null)
              {
                contentSource.ContentSourceType = ContentSourceType.Topic.ToString();
                contentSource.Content = model.Topic;
                contentSource.Url = null;
                contentSource.UploadedFileId = null;
                _context.Update(contentSource);
              }
              else
              {
                contentSource = new ContentSource
                {
                  ContentSourceType = ContentSourceType.Topic.ToString(),
                  Content = model.Topic,
                  QuestionSetId = id
                };
                _context.Add(contentSource);
              }
            }
            else if (!string.IsNullOrEmpty(model.TextContent))
            {
              if (contentSource != null)
              {
                contentSource.ContentSourceType = ContentSourceType.Text.ToString();
                contentSource.Content = model.TextContent;
                contentSource.Url = null;
                contentSource.UploadedFileId = null;
                _context.Update(contentSource);
              }
              else
              {
                contentSource = new ContentSource
                {
                  ContentSourceType = ContentSourceType.Text.ToString(),
                  Content = model.TextContent,
                  QuestionSetId = id
                };
                _context.Add(contentSource);
              }
            }
            else if (!string.IsNullOrEmpty(model.LinkUrl))
            {
              if (contentSource != null)
              {
                contentSource.ContentSourceType = ContentSourceType.Link.ToString();
                contentSource.Url = model.LinkUrl;
                contentSource.Content = null;
                contentSource.UploadedFileId = null;
                _context.Update(contentSource);
              }
              else
              {
                contentSource = new ContentSource
                {
                  ContentSourceType = ContentSourceType.Link.ToString(),
                  Url = model.LinkUrl,
                  QuestionSetId = id
                };
                _context.Add(contentSource);
              }
            }
            else if (!string.IsNullOrEmpty(model.YoutubeUrl))
            {
              if (contentSource != null)
              {
                contentSource.ContentSourceType = ContentSourceType.Youtube.ToString();
                contentSource.Url = model.YoutubeUrl;
                contentSource.Content = null;
                contentSource.UploadedFileId = null;
                _context.Update(contentSource);
              }
              else
              {
                contentSource = new ContentSource
                {
                  ContentSourceType = ContentSourceType.Youtube.ToString(),
                  Url = model.YoutubeUrl,
                  QuestionSetId = id
                };
                _context.Add(contentSource);
              }
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

      return View(questionSet);
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

    // POST: QuestionSets/AddToExam
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToExam(int questionSetId, int examId)
    {
      var result = await _questionGenerationService.AddQuestionsToExamAsync(questionSetId, examId);
      if (!result)
      {
        TempData["ErrorMessage"] = "فشل إضافة الأسئلة إلى الاختبار. تأكد أن مجموعة الأسئلة مكتملة وأن الاختبار موجود.";
        return RedirectToAction("Details", new { id = questionSetId });
      }

      TempData["SuccessMessage"] = "تم إضافة الأسئلة إلى الاختبار بنجاح.";
      return RedirectToAction("Details", "Exams", new { id = examId });
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
  }
}
