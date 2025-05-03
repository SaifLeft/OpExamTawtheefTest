using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using TawtheefTest.Services;
using TawtheefTest.ViewModels;
using TawtheefTest.DTOs;
using System.Threading.Tasks;
using System.Linq;
using TawtheefTest.Enum;
using TawtheefTest.DTOs.ExamModels;

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
          .Include(e => e.ExamQuestionSets)
              .ThenInclude(eqs => eqs.QuestionSet)
          .Select(e => new ExamDto
          {
            Id = e.Id,
            Name = e.Name,
            Description = e.Description,
            JobId = e.JobId,
            JobName = e.Job.Title,
            Duration = e.Duration ?? 60,
            StartDate = e.StartDate ?? DateTime.Now,
            EndDate = e.EndDate ?? DateTime.Now.AddDays(7),
            CreatedDate = e.CreatedAt,
            ApplicantsCount = e.CandidateExams.Count(),
            QuestionSets = e.ExamQuestionSets.Select(eqs => new QuestionSetDto
            {
              Id = eqs.QuestionSet.Id,
              Name = eqs.QuestionSet.Name,
              Status = eqs.QuestionSet.Status,
              QuestionCount = eqs.QuestionSet.QuestionCount
            }).ToList()
          })
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
          .Include(e => e.ExamQuestionSets)
              .ThenInclude(eqs => eqs.QuestionSet)
                  .ThenInclude(qs => qs.Questions)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (exam == null)
      {
        return NotFound();
      }

      var examDetailsDto = new ExamDetailsDTO
      {
        Id = exam.Id,
        Name = exam.Name,
        Description = exam.Description,
        JobId = exam.JobId,
        JobName = exam.Job.Title,
        Duration = exam.Duration ?? 60,
        CreatedDate = exam.CreatedAt,
        ExamStartDate = exam.StartDate ?? DateTime.Now,
        ExamEndDate = exam.EndDate ?? DateTime.Now.AddDays(7),
        QuestionSets = exam.ExamQuestionSets.Select(eqs => new QuestionSetDto
        {
          Id = eqs.QuestionSet.Id,
          Name = eqs.QuestionSet.Name,
          Description = eqs.QuestionSet.Description,
          QuestionType = eqs.QuestionSet.QuestionType,
          QuestionCount = eqs.QuestionSet.QuestionCount,
          Status = eqs.QuestionSet.Status,
          CreatedAt = eqs.QuestionSet.CreatedAt
        }).ToList()
      };

      return View(examDetailsDto);
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
          StartDate = model.StartDate,
          EndDate = model.EndDate,
          Status = ExamStatus.Draft,
          CreatedAt = DateTime.UtcNow
        };

        _context.Add(exam);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "تم إنشاء الاختبار بنجاح";
        return RedirectToAction(nameof(Details), new { id = exam.Id });
      }

      ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Title", model.JobId);
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

      var examDto = new ExamDto
      {
        Id = exam.Id,
        Name = exam.Name,
        Description = exam.Description,
        JobId = exam.JobId,
        Duration = exam.Duration ?? 60,
        StartDate = exam.StartDate ?? DateTime.Now,
        EndDate = exam.EndDate ?? DateTime.Now.AddDays(7)
      };

      ViewBag.Jobs = new SelectList(_context.Jobs, "Id", "Title", exam.JobId);
      return View(examDto);
    }

    // POST: Exams/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ExamDto examDto)
    {
      if (id != examDto.Id)
      {
        return NotFound();
      }

      if (ModelState.IsValid)
      {
        try
        {
          var exam = await _context.Exams.FindAsync(id);
          if (exam == null)
          {
            return NotFound();
          }

          exam.Name = examDto.Name;
          exam.Description = examDto.Description;
          exam.JobId = examDto.JobId;
          exam.Duration = examDto.Duration;
          exam.StartDate = examDto.StartDate;
          exam.EndDate = examDto.EndDate;
          exam.UpdatedAt = DateTime.UtcNow;

          _context.Update(exam);
          await _context.SaveChangesAsync();

          TempData["SuccessMessage"] = "تم تحديث الاختبار بنجاح";
        }
        catch (DbUpdateConcurrencyException)
        {
          if (!ExamExists(examDto.Id))
          {
            return NotFound();
          }
          else
          {
            throw;
          }
        }
        return RedirectToAction(nameof(Details), new { id });
      }

      ViewBag.Jobs = new SelectList(_context.Jobs, "Id", "Title", examDto.JobId);
      return View(examDto);
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

      ViewData["JobName"] = exam.Job.Title;

      var examDetailsDto = new ExamDetailsDto
      {
        Id = exam.Id,
        Name = exam.Name,
        Description = exam.Description,
        JobId = exam.JobId
      };

      return View(examDetailsDto);
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
        Name = $"Question Set: {topic}",
        Description = $"Generated for {exam.Name}",
        QuestionType = questionType.ToString(),
        Difficulty = difficulty,
        QuestionCount = questionCount,
        Status = QuestionSetStatus.Pending,
        CreatedAt = DateTime.UtcNow
      };

      _context.QuestionSets.Add(questionSet);
      await _context.SaveChangesAsync();

      // Create content source
      var contentSource = new ContentSource
      {
        ContentSourceType = ContentSourceType.Topic.ToString(),
        Content = topic,
        QuestionSetId = questionSet.Id,
        CreatedAt = DateTime.UtcNow
      };

      _context.ContentSources.Add(contentSource);
      await _context.SaveChangesAsync();

      // Link question set to exam
      var examQuestionSet = new ExamQuestionSet
      {
        ExamId = exam.Id,
        QuestionSetId = questionSet.Id,
        DisplayOrder = (exam.ExamQuestionSets?.Count ?? 0) + 1
      };

      _context.ExamQuestionSets.Add(examQuestionSet);
      await _context.SaveChangesAsync();

      // Generate questions
      await _opExamsService.GenerateQuestionsAsync(questionSet.Id);

      TempData["SuccessMessage"] = "تم بدء عملية توليد الأسئلة";
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

      var results = exam.CandidateExams.Select(ce => new ExamResultDto
      {
        Id = ce.Id,
        ExamId = ce.ExamId,
        ApplicantName = ce.Candidate.Name,
        StartTime = ce.StartTime,
        EndTime = ce.EndTime,
        Score = ce.Score.HasValue ? (double)ce.Score.Value : 0,
        Status = ce.Status.ToString()
      }).ToList();

      ViewBag.ExamName = exam.Name;
      ViewBag.ExamId = exam.Id;

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

      var exams = await _context.Exams
          .Where(e => e.JobId == id)
          .Include(e => e.Job)
          .Include(e => e.Questions)
          .Select(e => new DTOs.ExamModels.ExamListDTO
          {
            Id = e.Id,
            Name = e.Name,
            JobId = e.JobId,
            JobName = e.Job.Title,
            Duration = e.Duration ?? 60,
            QuestionsCount = e.Questions.Count,
            CreatedDate = e.CreatedAt
          })
          .ToListAsync();

      ViewBag.JobName = job.Title;
      ViewBag.JobId = job.Id;

      return View(exams);
    }

    private bool ExamExists(int id)
    {
      return _context.Exams.Any(e => e.Id == id);
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

    private string GetDifficultyName(string difficulty)
    {
      return difficulty switch
      {
        "easy" => "سهل",
        "medium" => "متوسط",
        "hard" => "صعب",
        _ => difficulty
      };
    }
  }
}
