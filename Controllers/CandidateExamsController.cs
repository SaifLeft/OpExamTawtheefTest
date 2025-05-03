using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using TawtheefTest.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

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
          .Include(e => e.Questions)
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
          .FirstOrDefaultAsync(ce => ce.CandidateId == candidateId &&
                                    ce.ExamId == id &&
                                    ce.Status == CandidateExamStatus.InProgress);

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
                  .ThenInclude(q => q.Options)
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.Questions)
                  .ThenInclude(q => q.MatchingPairs)
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.Questions)
                  .ThenInclude(q => q.OrderingItems)
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
        return Unauthorized();
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
          if (bool.TryParse(answer, out bool boolAnswer))
          {
            isCorrect = boolAnswer == question.TrueFalseAnswer;
          }
          break;

        case QuestionTypeEnum.ShortAnswer:
        case QuestionTypeEnum.FillInTheBlank:
          isCorrect = string.Equals(question.Answer, answer, StringComparison.OrdinalIgnoreCase);
          break;

        case QuestionTypeEnum.MultiSelect:
          try
          {
            var selectedOptions = JsonSerializer.Deserialize<List<int>>(answer);
            var correctOptions = question.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToList();
            isCorrect = selectedOptions != null && selectedOptions.OrderBy(x => x).SequenceEqual(correctOptions.OrderBy(x => x));
          }
          catch
          {
            isCorrect = false;
          }
          break;

        case QuestionTypeEnum.Matching:
          try
          {
            var pairs = JsonSerializer.Deserialize<Dictionary<string, string>>(answer);
            var correctPairs = question.MatchingPairs.ToDictionary(p => p.LeftItem, p => p.RightItem);
            isCorrect = pairs != null && pairs.OrderBy(x => x.Key).SequenceEqual(correctPairs.OrderBy(x => x.Key));
          }
          catch
          {
            isCorrect = false;
          }
          break;

        case QuestionTypeEnum.Ordering:
          try
          {
            var orderItems = JsonSerializer.Deserialize<List<string>>(answer);
            var correctOrder = question.OrderingItems.OrderBy(o => o.CorrectOrder).Select(o => o.Text).ToList();
            isCorrect = orderItems != null && orderItems.SequenceEqual(correctOrder);
          }
          catch
          {
            isCorrect = false;
          }
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

      // Calculate score
      var totalQuestions = candidateExam.Exam.Questions.Count;
      var correctAnswers = candidateExam.CandidateAnswers.Count(ca => ca.IsCorrect == true);
      var score = totalQuestions > 0 ? (decimal)correctAnswers / totalQuestions * 100 : 0;

      // Update candidate exam
      candidateExam.EndTime = DateTime.UtcNow;
      candidateExam.Score = score;
      candidateExam.Status = CandidateExamStatus.Completed;
      candidateExam.UpdatedAt = DateTime.UtcNow;

      _context.Update(candidateExam);
      await _context.SaveChangesAsync();

      TempData["SuccessMessage"] = "تم تسليم الاختبار بنجاح";
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
              .ThenInclude(e => e.Job)
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

    // GET: CandidateExams/ByCandidateId/5
    public async Task<IActionResult> ByCandidateId(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var candidateExams = await _context.CandidateExams
          .Where(ce => ce.CandidateId == id)
          .Include(ce => ce.Exam)
          .ToListAsync();

      ViewData["CandidateExams"] = candidateExams;

      return View();
    }

    // POST: CandidateExams/ReplaceQuestion
    [HttpPost]
    public async Task<IActionResult> ReplaceQuestion(int candidateExamId, int questionId)
    {
      // Check if candidate is logged in
      var candidateId = HttpContext.Session.GetInt32("CandidateId");
      if (candidateId == null)
      {
        return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });
      }

      // Get candidate exam
      var candidateExam = await _context.CandidateExams
          .Include(ce => ce.Exam)
              .ThenInclude(e => e.Questions)
          .FirstOrDefaultAsync(ce => ce.Id == candidateExamId && ce.CandidateId == candidateId);

      if (candidateExam == null)
      {
        return Json(new { success = false, message = "الاختبار غير موجود" });
      }

      if (candidateExam.Status == CandidateExamStatus.Completed)
      {
        return Json(new { success = false, message = "الاختبار مكتمل" });
      }

      // Check if question replacement is already used
      if (candidateExam.Status == CandidateExamStatus.InProgress && candidateExam.StartTime.HasValue)
      {
        var questionReplaced = await _context.CandidateExams
            .Where(ce => ce.Id == candidateExamId)
            .Select(ce => ce.StartTime.HasValue && ce.StartTime.Value.AddMinutes(1) < DateTime.UtcNow)
            .FirstOrDefaultAsync();

        if (questionReplaced)
        {
          return Json(new { success = false, message = "تم استخدام استبدال السؤال بالفعل" });
        }
      }

      // Get the question to replace
      var questionToReplace = await _context.Questions
          .FirstOrDefaultAsync(q => q.Id == questionId && q.ExamId == candidateExam.ExamId);

      if (questionToReplace == null)
      {
        return Json(new { success = false, message = "السؤال غير موجود" });
      }

      // Find a replacement question of the same type and difficulty
      var replacementQuestion = await _context.Questions
          .Where(q => q.ExamId == candidateExam.ExamId &&
                     q.QuestionType == questionToReplace.QuestionType &&
                     q.Id != questionId &&
                     !_context.CandidateAnswers.Any(ca => ca.CandidateExamId == candidateExamId && ca.QuestionId == q.Id))
          .OrderBy(q => Guid.NewGuid()) // Random selection
          .FirstOrDefaultAsync();

      if (replacementQuestion == null)
      {
        return Json(new { success = false, message = "لا يوجد سؤال بديل متاح" });
      }

      // Delete existing answer if any
      var existingAnswer = await _context.CandidateAnswers
          .FirstOrDefaultAsync(ca => ca.CandidateExamId == candidateExamId && ca.QuestionId == questionId);

      if (existingAnswer != null)
      {
        _context.CandidateAnswers.Remove(existingAnswer);
        await _context.SaveChangesAsync();
      }

      return Json(new { success = true, replacementQuestionId = replacementQuestion.Id });
    }
  }
}
