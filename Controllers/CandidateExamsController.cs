using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using OpExamTawtheefTest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TawtheefTest.Controllers
{
    public class CandidateExamsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CandidateExamsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: CandidateExams
        public async Task<IActionResult> Index()
        {
            // Check if candidate is logged in
            var candidateId = HttpContext.Session.GetInt32("CandidateId");
            if (candidateId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Get candidate details
            var candidate = await _context.Candidates
                .Include(c => c.Job)
                .FirstOrDefaultAsync(c => c.Id == candidateId);

            if (candidate == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Get all exams for the candidate's job
            var exams = await _context.Exams
                .Where(e => e.JobId == candidate.JobId)
                .Include(e => e.Job)
                .ToListAsync();

            // Get candidate's exam attempts
            var candidateExams = await _context.CandidateExams
                .Where(ce => ce.CandidateId == candidateId)
                .Include(ce => ce.Exam)
                .ToListAsync();

            ViewData["Candidate"] = candidate;
            ViewData["CandidateExams"] = candidateExams;

            return View(exams);
        }

        // GET: CandidateExams/Start/5
        public async Task<IActionResult> Start(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Check if candidate is logged in
            var candidateId = HttpContext.Session.GetInt32("CandidateId");
            if (candidateId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Get exam details
            var exam = await _context.Exams
                .Include(e => e.Job)
                .Include(e => e.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null)
            {
                return NotFound();
            }

            // Check if candidate already has an unfinished exam attempt
            var existingAttempt = await _context.CandidateExams
                .FirstOrDefaultAsync(ce => ce.CandidateId == candidateId && ce.ExamId == id && !ce.IsCompleted);

            if (existingAttempt != null)
            {
                // Continue existing attempt
                return RedirectToAction(nameof(Take), new { id = existingAttempt.Id });
            }

            // Create new exam attempt
            var candidateExam = new CandidateExam
            {
                CandidateId = candidateId.Value,
                ExamId = id.Value,
                StartTime = DateTime.UtcNow,
                IsCompleted = false
            };

            _context.CandidateExams.Add(candidateExam);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Take), new { id = candidateExam.Id });
        }

        // GET: CandidateExams/Take/5
        public async Task<IActionResult> Take(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Check if candidate is logged in
            var candidateId = HttpContext.Session.GetInt32("CandidateId");
            if (candidateId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Get candidate exam details
            var candidateExam = await _context.CandidateExams
                .Include(ce => ce.Candidate)
                .Include(ce => ce.Exam)
                    .ThenInclude(e => e.Questions)
                        .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(ce => ce.Id == id && ce.CandidateId == candidateId);

            if (candidateExam == null)
            {
                return NotFound();
            }

            if (candidateExam.IsCompleted)
            {
                return RedirectToAction(nameof(Results), new { id = candidateExam.Id });
            }

            // Get candidate's answers
            var candidateAnswers = await _context.CandidateAnswers
                .Where(ca => ca.CandidateExamId == id)
                .ToListAsync();

            ViewData["CandidateAnswers"] = candidateAnswers;

            return View(candidateExam);
        }

        // POST: CandidateExams/SaveAnswer
        [HttpPost]
        public async Task<IActionResult> SaveAnswer(int candidateExamId, int questionId, string answer)
        {
            // Check if candidate is logged in
            var candidateId = HttpContext.Session.GetInt32("CandidateId");
            if (candidateId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Get candidate exam
            var candidateExam = await _context.CandidateExams
                .FirstOrDefaultAsync(ce => ce.Id == candidateExamId && ce.CandidateId == candidateId);

            if (candidateExam == null || candidateExam.IsCompleted)
            {
                return BadRequest();
            }

            // Get question
            var question = await _context.Questions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == questionId && q.ExamId == candidateExam.ExamId);

            if (question == null)
            {
                return BadRequest();
            }

            // Check if answer already exists
            var existingAnswer = await _context.CandidateAnswers
                .FirstOrDefaultAsync(ca => ca.CandidateExamId == candidateExamId && ca.QuestionId == questionId);

            bool isCorrect = false;

            // Check if answer is correct
            switch (question.QuestionType.ToUpper())
            {
                case "MCQ":
                case "TF":
                    var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
                    isCorrect = correctOption != null && correctOption.Id.ToString() == answer;
                    break;

                case "OPEN":
                case "SHORTANSWER":
                case "FILLINTHEBLANK":
                    isCorrect = string.Equals(question.CorrectAnswer, answer, StringComparison.OrdinalIgnoreCase);
                    break;

                    // Add more cases for other question types as needed
            }

            if (existingAnswer != null)
            {
                // Update existing answer
                existingAnswer.Answer = answer;
                existingAnswer.IsCorrect = isCorrect;
                _context.Update(existingAnswer);
            }
            else
            {
                // Create new answer
                var candidateAnswer = new CandidateAnswer
                {
                    CandidateExamId = candidateExamId,
                    QuestionId = questionId,
                    Answer = answer,
                    IsCorrect = isCorrect
                };

                _context.CandidateAnswers.Add(candidateAnswer);
            }

            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: CandidateExams/Submit
        [HttpPost]
        public async Task<IActionResult> Submit(int id)
        {
            // Check if candidate is logged in
            var candidateId = HttpContext.Session.GetInt32("CandidateId");
            if (candidateId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Get candidate exam
            var candidateExam = await _context.CandidateExams
                .Include(ce => ce.Exam)
                .FirstOrDefaultAsync(ce => ce.Id == id && ce.CandidateId == candidateId);

            if (candidateExam == null)
            {
                return NotFound();
            }

            if (candidateExam.IsCompleted)
            {
                return RedirectToAction(nameof(Results), new { id = candidateExam.Id });
            }

            // Get candidate answers
            var candidateAnswers = await _context.CandidateAnswers
                .Where(ca => ca.CandidateExamId == id)
                .ToListAsync();

            // Calculate score
            int totalQuestions = await _context.Questions.CountAsync(q => q.ExamId == candidateExam.ExamId);
            int correctAnswers = candidateAnswers.Count(ca => ca.IsCorrect);
            double score = totalQuestions > 0 ? (double)correctAnswers / totalQuestions * 100 : 0;

            // Update candidate exam
            candidateExam.EndTime = DateTime.UtcNow;
            candidateExam.Score = score;
            candidateExam.IsCompleted = true;

            _context.Update(candidateExam);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Results), new { id = candidateExam.Id });
        }

        // GET: CandidateExams/Results/5
        public async Task<IActionResult> Results(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Check if candidate is logged in
            var candidateId = HttpContext.Session.GetInt32("CandidateId");
            if (candidateId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Get candidate exam details
            var candidateExam = await _context.CandidateExams
                .Include(ce => ce.Candidate)
                .Include(ce => ce.Exam)
                .FirstOrDefaultAsync(ce => ce.Id == id && ce.CandidateId == candidateId);

            if (candidateExam == null)
            {
                return NotFound();
            }

            // Get candidate answers
            var candidateAnswers = await _context.CandidateAnswers
                .Where(ca => ca.CandidateExamId == id)
                .Include(ca => ca.Question)
                    .ThenInclude(q => q.Options)
                .ToListAsync();

            ViewData["CandidateAnswers"] = candidateAnswers;

            return View(candidateExam);
        }
    }
}