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

namespace TawtheefTest.Controllers
{
  public class QuestionSetsController : Controller
  {
    private readonly ApplicationDbContext _context;
    private readonly IOpExamsService _opExamsService;

    public QuestionSetsController(ApplicationDbContext context, IOpExamsService opExamsService)
    {
      _context = context;
      _opExamsService = opExamsService;
    }

    // GET: QuestionSets
    public async Task<IActionResult> Index()
    {
      var questionSets = await _context.QuestionSets
          .Include(qs => qs.ExamQuestionSets)
              .ThenInclude(eqs => eqs.Exam)
          .Select(qs => new QuestionSetDto
          {
            Id = qs.Id,
            Name = qs.Name,
            Description = qs.Description,
            QuestionType = qs.QuestionType.ToString(),
            QuestionCount = qs.QuestionCount,
            Status = qs.Status.ToString(),
            CreatedDate = qs.CreatedAt,
            CompletedDate = qs.ProcessedAt
          })
          .ToListAsync();

      return View(questionSets);
    }

    // GET: QuestionSets/Details/5
    public async Task<IActionResult> Details(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var questionSet = await _context.QuestionSets
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.Options)
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.MatchingPairs)
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.OrderingItems)
          .Include(qs => qs.ContentSources)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (questionSet == null)
      {
        return NotFound();
      }

      var questionSetDto = new QuestionSetDto
      {
        Id = questionSet.Id,
        Name = questionSet.Name,
        Description = questionSet.Description,
        QuestionType = questionSet.QuestionType.ToString(),
        Language = questionSet.Language,
        Difficulty = questionSet.Difficulty,
        QuestionCount = questionSet.QuestionCount,
        OptionsCount = questionSet.OptionsCount ?? 4,
        Status = questionSet.Status.ToString(),
        CreatedDate = questionSet.CreatedAt,
        CompletedDate = questionSet.ProcessedAt,
        ContentType = questionSet.ContentSources.FirstOrDefault()?.ContentSourceType.ToString(),
      };

      ViewBag.Questions = questionSet.Questions.ToList();
      return View(questionSetDto);
    }

    // GET: QuestionSets/Create
    public IActionResult Create(int examId)
    {
      ViewBag.Exams = new SelectList(_context.Exams.ToList(), "Id", "Name");
      return View();
    }

    // POST: QuestionSets/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateQuestionSetViewModel model)
    {
      if (ModelState.IsValid)
      {
        var questionSet = new QuestionSet
        {
          Name = model.Name,
          Description = model.Description,
          QuestionType = model.QuestionType.ToString(),
          Language = model.Language,
          Difficulty = model.Difficulty,
          QuestionCount = model.QuestionCount,
          OptionsCount = model.OptionsCount,
          NumberOfRows = model.NumberOfRows,
          NumberOfCorrectOptions = model.NumberOfCorrectOptions,
          Status = QuestionSetStatus.Pending,
          CreatedAt = DateTime.UtcNow
        };

        _context.Add(questionSet);
        await _context.SaveChangesAsync();

        if (model.ExamId > 0)
        {
          var examQuestionSet = new ExamQuestionSet
          {
            ExamId = model.ExamId,
            QuestionSetId = questionSet.Id,
            DisplayOrder = 1
          };

          _context.Add(examQuestionSet);
          await _context.SaveChangesAsync();
        }

        ContentSource contentSource = null;

        if (!string.IsNullOrEmpty(model.Topic))
        {
          contentSource = new ContentSource
          {
            ContentSourceType = ContentSourceType.Topic.ToString(),
            Content = model.Topic,
            QuestionSetId = questionSet.Id
          };
        }
        else if (!string.IsNullOrEmpty(model.TextContent))
        {
          contentSource = new ContentSource
          {
            ContentSourceType = ContentSourceType.Text.ToString(),
            Content = model.TextContent,
            QuestionSetId = questionSet.Id
          };
        }
        else if (!string.IsNullOrEmpty(model.LinkUrl))
        {
          contentSource = new ContentSource
          {
            ContentSourceType = ContentSourceType.Link.ToString(),
            Url = model.LinkUrl,
            QuestionSetId = questionSet.Id
          };
        }
        else if (!string.IsNullOrEmpty(model.YoutubeUrl))
        {
          contentSource = new ContentSource
          {
            ContentSourceType = ContentSourceType.Youtube.ToString(),
            Url = model.YoutubeUrl,
            QuestionSetId = questionSet.Id
          };
        }
        else if (!string.IsNullOrEmpty(model.FileReference))
        {
          ContentSourceType contentType = ContentSourceType.Document;
          var contentTypeStr = Request.Form["ContentType"].ToString();

          if (System.Enum.TryParse(contentTypeStr, true, out ContentSourceType parsedType))
          {
            contentType = parsedType;
          }

          contentSource = new ContentSource
          {
            ContentSourceType = contentType.ToString(),
            QuestionSetId = questionSet.Id
          };

          if (!string.IsNullOrEmpty(model.FileReference))
          {
            // هنا يمكن إضافة معالجة للملف المحمل
            // على سبيل المثال الحصول على معرّف الملف من المرجع وإضافته إلى مصدر المحتوى
          }
        }

        if (contentSource != null)
        {
          _context.Add(contentSource);
          await _context.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = "تم إنشاء مجموعة الأسئلة بنجاح";
        return RedirectToAction(nameof(Index));
      }

      ViewBag.Exams = new SelectList(_context.Exams.ToList(), "Id", "Name");
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
    public async Task<IActionResult> Edit(int id, QuestionSet questionSet)
    {
      if (id != questionSet.Id)
      {
        return NotFound();
      }

      if (ModelState.IsValid)
      {
        try
        {
          questionSet.UpdatedAt = DateTime.UtcNow;
          _context.Update(questionSet);
          await _context.SaveChangesAsync();

          TempData["SuccessMessage"] = "تم تحديث مجموعة الأسئلة بنجاح";
        }
        catch (DbUpdateConcurrencyException)
        {
          if (!QuestionSetExists(questionSet.Id))
          {
            return NotFound();
          }
          else
          {
            throw;
          }
        }
        return RedirectToAction(nameof(Index));
      }

      ViewBag.Exams = new SelectList(_context.Exams.ToList(), "Id", "Name");
      return View(questionSet);
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
          .FirstOrDefaultAsync(qs => qs.Id == id);

      if (questionSet != null)
      {
        _context.QuestionSets.Remove(questionSet);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "تم حذف مجموعة الأسئلة بنجاح";
      }

      return RedirectToAction(nameof(Index));
    }

    private bool QuestionSetExists(int id)
    {
      return _context.QuestionSets.Any(e => e.Id == id);
    }
  }
}
