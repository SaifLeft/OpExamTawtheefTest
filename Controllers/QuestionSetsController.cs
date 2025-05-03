using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using TawtheefTest.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace TawtheefTest.Controllers
{
  public class QuestionSetsController : Controller
  {
    private readonly ApplicationDbContext _context;

    public QuestionSetsController(ApplicationDbContext context)
    {
      _context = context;
    }

    // GET: QuestionSets
    public async Task<IActionResult> Index()
    {
      var questionSets = await _context.QuestionSets
          .Include(qs => qs.ExamQuestionSets)
              .ThenInclude(eqs => eqs.Exam)
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
          .Include(qs => qs.ExamQuestionSets)
              .ThenInclude(eqs => eqs.Exam)
          .Include(qs => qs.Questions)
              .ThenInclude(q => q.Options)
          .Include(qs => qs.ContentSources)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (questionSet == null)
      {
        return NotFound();
      }

      return View(questionSet);
    }

    // GET: QuestionSets/Create
    public IActionResult Create()
    {
      ViewBag.Exams = _context.Exams.ToList();
      ViewBag.QuestionTypes = GetQuestionTypesList();
      return View();
    }

    // POST: QuestionSets/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateQuestionSetDto model)
    {
      if (ModelState.IsValid)
      {
        var questionSet = new QuestionSet
        {
          Name = model.Name,
          Description = model.Description,
          QuestionType = model.QuestionType,
          Difficulty = model.Difficulty,
          QuestionCount = model.QuestionCount,
          OptionsCount = model.OptionsCount,
          Status = QuestionSetStatus.Pending,
          CreatedAt = System.DateTime.UtcNow
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

        return RedirectToAction(nameof(Index));
      }

      ViewBag.Exams = _context.Exams.ToList();
      ViewBag.QuestionTypes = GetQuestionTypesList();
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
      ViewBag.QuestionTypes = GetQuestionTypesList();
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
          questionSet.UpdatedAt = System.DateTime.UtcNow;
          _context.Update(questionSet);
          await _context.SaveChangesAsync();
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
      ViewBag.QuestionTypes = GetQuestionTypesList();
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
      var questionSet = await _context.QuestionSets.FindAsync(id);
      _context.QuestionSets.Remove(questionSet);
      await _context.SaveChangesAsync();
      return RedirectToAction(nameof(Index));
    }

    private bool QuestionSetExists(int id)
    {
      return _context.QuestionSets.Any(e => e.Id == id);
    }

    private System.Collections.Generic.List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> GetQuestionTypesList()
    {
      return System.Enum.GetValues(typeof(QuestionTypeEnum))
          .Cast<QuestionTypeEnum>()
          .Select(qt => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
          {
            Value = qt.ToString(),
            Text = GetQuestionTypeName(qt)
          })
          .ToList();
    }

    private string GetQuestionTypeName(QuestionTypeEnum type)
    {
      return type switch
      {
        QuestionTypeEnum.MCQ => "اختيار من متعدد",
        QuestionTypeEnum.TF => "صح/خطأ",
        QuestionTypeEnum.Open => "إجابة مفتوحة",
        QuestionTypeEnum.FillInTheBlank => "ملء الفراغات",
        QuestionTypeEnum.Ordering => "ترتيب",
        QuestionTypeEnum.Matching => "مطابقة",
        QuestionTypeEnum.MultiSelect => "اختيار متعدد",
        QuestionTypeEnum.ShortAnswer => "إجابة قصيرة",
        _ => type.ToString()
      };
    }
  }
}
