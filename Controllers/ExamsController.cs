using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TawtheefTest.Models;
using TawtheefTest.Services;

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
            ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Name");
            ViewData["QuestionTypes"] = GetQuestionTypesList();
            ViewData["DifficultiesTypes"] = GetDifficultiesList();
            return View();
        }

        // POST: Exams/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("JobId,QuestionCount,Difficulty,OptionsCount,QuestionType")] Exam exam)
        {
            if (ModelState.IsValid)
            {
                exam.CreatedDate = DateTime.UtcNow;
                _context.Add(exam);
                await _context.SaveChangesAsync();

                // Redirect to generate questions
                return RedirectToAction(nameof(GenerateQuestions), new { id = exam.Id });
            }

            ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Name", exam.JobId);
            ViewData["QuestionTypes"] = GetQuestionTypesList();
            ViewData["DifficultiesTypes"] = GetDifficultiesList();
            return View(exam);
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

            ViewData["ExamId"] = exam.Id;
            ViewData["JobName"] = exam.Job.Name;
            return View(exam);
        }

        // POST: Exams/GenerateQuestions/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateQuestions(int id, string topic)
        {
            var exam = await _context.Exams
                .Include(e => e.Job)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (exam == null)
            {
                return NotFound();
            }

            try
            {
                // Delete existing questions if any
                var existingQuestions = await _context.Questions
                    .Where(q => q.ExamId == id)
                    .ToListAsync();

                if (existingQuestions.Any())
                {
                    _context.Questions.RemoveRange(existingQuestions);
                    await _context.SaveChangesAsync();
                }

                // Generate questions using the OpExams API
                var questions = await _opExamsService.GenerateQuestions(
                    topic,
                    exam.QuestionType,
                    "English", // Using English as default language
                    exam.Difficulty,
                    exam.QuestionCount,
                    exam.OptionsCount);

                if (questions != null && questions.Any())
                {
                    foreach (var question in questions)
                    {
                        question.ExamId = id;
                        _context.Questions.Add(question);
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Questions generated successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to generate questions. Please try again.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id = exam.Id });
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

            ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Name", exam.JobId);
            ViewData["QuestionTypes"] = GetQuestionTypesList();
            ViewData["DifficultiesTypes"] = GetDifficultiesList();
            return View(exam);
        }

        // POST: Exams/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,JobId,QuestionCount,Difficulty,OptionsCount,QuestionType,CreatedDate")] Exam exam)
        {
            if (id != exam.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
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

            ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Name", exam.JobId);
            ViewData["QuestionTypes"] = GetQuestionTypesList();
            ViewData["DifficultiesTypes"] = GetDifficultiesList();
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
            if (exam != null)
            {
                _context.Exams.Remove(exam);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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
                .ToListAsync();

            ViewData["JobName"] = job.Name;
            ViewData["JobId"] = job.Id;

            return View(exams);
        }

        private bool ExamExists(int id)
        {
            return _context.Exams.Any(e => e.Id == id);
        }

        private List<SelectListItem> GetQuestionTypesList()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "MCQ", Text = "Multiple Choice" },
                new SelectListItem { Value = "TF", Text = "True/False" },
                new SelectListItem { Value = "open", Text = "Open-Ended" },
                new SelectListItem { Value = "fillInTheBlank", Text = "Fill in the Blank" },
                new SelectListItem { Value = "ordering", Text = "Ordering" },
                new SelectListItem { Value = "matching", Text = "Matching" },
                new SelectListItem { Value = "multiSelect", Text = "Multi-Select" },
                new SelectListItem { Value = "shortAnswer", Text = "Short Answer" }
            };
        }

        private List<SelectListItem> GetDifficultiesList()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "easy", Text = "Easy" },
                new SelectListItem { Value = "medium", Text = "Medium" },
                new SelectListItem { Value = "hard", Text = "Hard" }
            };
        }
    }
}