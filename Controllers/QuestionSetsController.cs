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
        CompletedDate = questionSet.ProcessedAt
      };

      ViewBag.Questions = questionSet.Questions.ToList();
      return View(questionSetDto);
    }

    // GET: QuestionSets/Create
    public IActionResult Create()
    {
      ViewBag.Exams = _context.Exams.ToList();
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
          Difficulty = model.Difficulty,
          QuestionCount = model.QuestionCount,
          OptionsCount = model.OptionsCount,
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

        TempData["SuccessMessage"] = "تم إنشاء مجموعة الأسئلة بنجاح";
        return RedirectToAction(nameof(Index));
      }

      ViewBag.Exams = _context.Exams.ToList();
      return View(model);
    }

    // GET: QuestionSets/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var questionSet = await _context.QuestionSets.FindAsync(id);
      if (questionSet == null)
      {
        return NotFound();
      }

      ViewBag.Exams = _context.Exams.ToList();
      return View(questionSet);
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

      ViewBag.Exams = _context.Exams.ToList();
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
