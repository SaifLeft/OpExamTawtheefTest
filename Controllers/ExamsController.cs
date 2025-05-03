using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using TawtheefTest.Services;
using TawtheefTest.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TawtheefTest.Controllers
{
  public class ExamsController : Controller
  {
    private readonly ApplicationDbContext _context;
    private readonly IOpExamsService _opExamsService;

    public ExamsController(ApplicationDbContext context, IOpExamsService opExamsService)
    {
      _context = context;
      _opExamsService = opExamsService;
    }

    // GET: Exams
    public async Task<IActionResult> Index()
    {
      var exams = await _context.Exams
          .Include(e => e.Job)
          .ToListAsync();
      return View(exams);
    }

    // GET: Exams/Details/5
    public async Task<IActionResult> Details(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var exam = await _context.Exams
          .Include(e => e.Job)
          .Include(e => e.Questions)
              .ThenInclude(q => q.Options)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (exam == null)
      {
        return NotFound();
      }

      return View(exam);
    }

    // GET: Exams/Create
    public IActionResult Create()
    {
      ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Title");
      ViewData["QuestionTypes"] = GetQuestionTypesList();
      ViewData["Difficulties"] = GetDifficultiesList();
      return View();
    }

    // POST: Exams/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateExamDto model)
    {
      if (ModelState.IsValid)
      {
        var exam = new Exam
        {
          Name = model.Name,
          Description = model.Description,
          JobId = model.JobId,
          Duration = model.Duration,
          Status = ExamStatus.Draft,
          CreatedAt = DateTime.UtcNow
        };

        _context.Add(exam);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = exam.Id });
      }

      ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Title", model.JobId);
      ViewData["QuestionTypes"] = GetQuestionTypesList();
      ViewData["Difficulties"] = GetDifficultiesList();
      return View(model);
    }

    // GET: Exams/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var exam = await _context.Exams.FindAsync(id);
      if (exam == null)
      {
        return NotFound();
      }

      ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Title", exam.JobId);
      return View(exam);
    }

    // POST: Exams/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Exam exam)
    {
      if (id != exam.Id)
      {
        return NotFound();
      }

      if (ModelState.IsValid)
      {
        try
        {
          exam.UpdatedAt = DateTime.UtcNow;
          _context.Update(exam);
          await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
          if (!ExamExists(exam.Id))
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

      ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Title", exam.JobId);
      return View(exam);
    }

    // GET: Exams/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var exam = await _context.Exams
          .Include(e => e.Job)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (exam == null)
      {
        return NotFound();
      }

      return View(exam);
    }

    // POST: Exams/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      var exam = await _context.Exams.FindAsync(id);
      _context.Exams.Remove(exam);
      await _context.SaveChangesAsync();
      return RedirectToAction(nameof(Index));
    }

    // GET: Exams/GenerateQuestions/5
    public async Task<IActionResult> GenerateQuestions(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var exam = await _context.Exams
          .Include(e => e.Job)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (exam == null)
      {
        return NotFound();
      }

      // Create a new QuestionSet
      var questionSet = new QuestionSet
      {
        Name = $"Question Set for {exam.Name}",
        Description = $"Generated for {exam.Name}",
        QuestionType = QuestionTypeEnum.MCQ,
        QuestionCount = 10,
        Status = QuestionSetStatus.Pending,
        CreatedAt = DateTime.UtcNow
      };

      ViewData["JobName"] = exam.Job.Title;
      return View(new ExamDetailsDto
      {
        Id = exam.Id,
        Name = exam.Name,
        Description = exam.Description,
        JobId = exam.JobId
      });
    }

    // POST: Exams/GenerateQuestions/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateQuestions(int id, string topic, QuestionTypeEnum questionType, string difficulty, int questionCount)
    {
      var exam = await _context.Exams
          .Include(e => e.Questions)
          .FirstOrDefaultAsync(e => e.Id == id);

      if (exam == null)
      {
        return NotFound();
      }

      // Create a new QuestionSet
      var questionSet = new QuestionSet
      {
        Name = $"Question Set from Topic: {topic}",
        Description = $"Generated for {exam.Name}",
        QuestionType = questionType,
        Difficulty = difficulty,
        QuestionCount = questionCount,
        Status = QuestionSetStatus.Pending,
        CreatedAt = DateTime.UtcNow
      };

      _context.QuestionSets.Add(questionSet);
      await _context.SaveChangesAsync();

      // Create a ContentSource
      var contentSource = new ContentSource
      {
        Type = ContentSourceType.Topic,
        Content = topic,
        QuestionSetId = questionSet.Id,
        CreatedAt = DateTime.UtcNow
      };

      _context.ContentSources.Add(contentSource);
      await _context.SaveChangesAsync();

      // Create an ExamQuestionSet relation
      var examQuestionSet = new ExamQuestionSet
      {
        ExamId = exam.Id,
        QuestionSetId = questionSet.Id,
        DisplayOrder = 1
      };

      _context.ExamQuestionSets.Add(examQuestionSet);
      await _context.SaveChangesAsync();

      // Generate questions using the service
      var result = await _opExamsService.GenerateQuestionsAsync(questionSet.Id);

      return RedirectToAction(nameof(Details), new { id });
    }

    // GET: Exams/Results/5
    public async Task<IActionResult> Results(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var exam = await _context.Exams
          .Include(e => e.CandidateExams)
              .ThenInclude(ce => ce.Candidate)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (exam == null)
      {
        return NotFound();
      }

      return View(exam);
    }

    // GET: Exams/ResultDetails/5
    public async Task<IActionResult> ResultDetails(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var candidateExam = await _context.CandidateExams
          .Include(ce => ce.Candidate)
          .Include(ce => ce.Exam)
          .Include(ce => ce.CandidateAnswers)
              .ThenInclude(a => a.Question)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (candidateExam == null)
      {
        return NotFound();
      }

      return View(candidateExam);
    }

    // GET: Exams/ByJob/5
    public async Task<IActionResult> ByJob(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var exams = await _context.Exams
          .Where(e => e.JobId == id)
          .Include(e => e.Job)
          .ToListAsync();

      var job = await _context.Jobs.FindAsync(id);
      ViewData["JobName"] = job?.Title;
      return View(exams);
    }

    private bool ExamExists(int id)
    {
      return _context.Exams.Any(e => e.Id == id);
    }

    private List<SelectListItem> GetQuestionTypesList()
    {
      return Enum.GetValues(typeof(QuestionTypeEnum))
          .Cast<QuestionTypeEnum>()
          .Select(qt => new SelectListItem
          {
            Value = qt.ToString(),
            Text = GetQuestionTypeName(qt)
          })
          .ToList();
    }

    private List<SelectListItem> GetDifficultiesList()
    {
      return new List<SelectListItem>
      {
        new SelectListItem { Value = "easy", Text = "سهل" },
        new SelectListItem { Value = "medium", Text = "متوسط" },
        new SelectListItem { Value = "hard", Text = "صعب" }
      };
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
