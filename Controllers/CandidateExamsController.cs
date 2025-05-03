using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
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
          .FirstOrDefaultAsync(e => e.Id == id);

      if (exam == null)
      {
        return NotFound();
      }

      // Check if candidate already has an unfinished exam attempt
      var existingAttempt = await _context.CandidateExams
          .FirstOrDefaultAsync(ce => ce.CandidateId == candidateId && ce.ExamId == id && ce.Status == CandidateExamStatus.InProgress);

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
        Status = CandidateExamStatus.InProgress,
        CreatedAt = DateTime.UtcNow
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
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.Questions)
          .Include(ce => ce.CandidateAnswers)
          .FirstOrDefaultAsync(ce => ce.Id == id && ce.CandidateId == candidateId);

      if (candidateExam == null)
      {
        return NotFound();
      }

      if (candidateExam.Status == CandidateExamStatus.Completed)
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

      if (candidateExam == null || candidateExam.Status == CandidateExamStatus.Completed)
      {
        return BadRequest();
      }

      // Get question
      var question = await _context.Questions
          .Include(q => q.Options)
          .FirstOrDefaultAsync(q => q.Id == questionId);

      if (question == null)
      {
        return BadRequest();
      }

      // Check if answer already exists
      var existingAnswer = await _context.CandidateAnswers
          .FirstOrDefaultAsync(ca => ca.CandidateExamId == candidateExamId && ca.QuestionId == questionId);

      bool? isCorrect = null;

      // Check if answer is correct
      switch (question.QuestionType)
      {
        case QuestionTypeEnum.MCQ:
          var selectedOption = question.Options.FirstOrDefault(o => o.Id.ToString() == answer);
          isCorrect = selectedOption?.IsCorrect;
          break;

        case QuestionTypeEnum.TF:
          bool boolAnswer;
          if (bool.TryParse(answer, out boolAnswer))
          {
            isCorrect = boolAnswer == question.TrueFalseAnswer;
          }
          break;

        case QuestionTypeEnum.ShortAnswer:
        case QuestionTypeEnum.FillInTheBlank:
          isCorrect = string.Equals(question.Answer, answer, StringComparison.OrdinalIgnoreCase);
          break;
      }

      if (existingAnswer != null)
      {
        // Update existing answer
        existingAnswer.AnswerText = answer;
        existingAnswer.IsCorrect = isCorrect;
        existingAnswer.UpdatedAt = DateTime.UtcNow;
        _context.Update(existingAnswer);
      }
      else
      {
        // Create new answer
        var candidateAnswer = new CandidateAnswer
        {
          CandidateExamId = candidateExamId,
          QuestionId = questionId,
          AnswerText = answer,
          IsCorrect = isCorrect,
          CreatedAt = DateTime.UtcNow
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
          .Include(ce => ce.CandidateAnswers)
          .FirstOrDefaultAsync(ce => ce.Id == id && ce.CandidateId == candidateId);

      if (candidateExam == null)
      {
        return NotFound();
      }

      if (candidateExam.Status == CandidateExamStatus.Completed)
      {
        return RedirectToAction(nameof(Results), new { id = candidateExam.Id });
      }

      // Calculate score
      var totalQuestions = await _context.Questions.CountAsync(q => q.ExamId == candidateExam.ExamId);
      var correctAnswers = candidateExam.CandidateAnswers.Count(ca => ca.IsCorrect == true);
      var score = totalQuestions > 0 ? (decimal)correctAnswers / totalQuestions * 100 : 0;

      // Update candidate exam
      candidateExam.EndTime = DateTime.UtcNow;
      candidateExam.Score = score;
      candidateExam.Status = CandidateExamStatus.Completed;
      candidateExam.UpdatedAt = DateTime.UtcNow;

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
