using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using TawtheefTest.Services;
using TawtheefTest.ViewModels;
using TawtheefTest.DTOs;
using System.Threading.Tasks;
using System.Linq;
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
              Status = eqs.QuestionSet.Status.ToString(),
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
        Duration = exam.Duration ?? 60,
        CreatedDate = exam.CreatedAt,
        ExamStartDate = exam.StartDate ?? DateTime.Now,
        ExamEndDate = exam.EndDate ?? DateTime.Now.AddDays(7),
        QuestionSets = exam.ExamQuestionSets.Select(eqs => new QuestionSetDto
        {
          Id = eqs.QuestionSet.Id,
          Name = eqs.QuestionSet.Name,
          Description = eqs.QuestionSet.Description,
          QuestionType = eqs.QuestionSet.QuestionType.ToString(),
          QuestionCount = eqs.QuestionSet.QuestionCount,
          Status = eqs.QuestionSet.Status.ToString(),
          CreatedDate = eqs.QuestionSet.CreatedAt
        }).ToList()
      };

      ViewBag.JobName = exam.Job.Title;
      return View(examDetailsDto);
    }

    // GET: Exams/Create
    public IActionResult Create()
    {
      ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Title");
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
          StartDate = model.ExamStartDate,
          EndDate = model.ExamEndDate,
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
        QuestionType = questionType,
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
        Type = ContentSourceType.Topic,
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
        ApplicantEmail = ce.Candidate.Email,
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

    // GET: Exams/PreviewExam/5
    public async Task<IActionResult> PreviewExam(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var exam = await _context.Exams
          .Include(e => e.Job)
          .Include(e => e.Questions)
              .ThenInclude(q => q.Options)
          .Include(e => e.Questions)
              .ThenInclude(q => q.MatchingPairs)
          .Include(e => e.Questions)
              .ThenInclude(q => q.OrderingItems)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (exam == null)
      {
        return NotFound();
      }

      var examDetailsDto = new DTOs.ExamModels.ExamDetailsDTO
      {
        Id = exam.Id,
        Name = exam.Name,
        Description = exam.Description,
        JobId = exam.JobId,
        Duration = exam.Duration ?? 60,
        QuestionCount = exam.Questions.Count,
        Difficulty = exam.Questions.FirstOrDefault()?.QuestionSet?.Difficulty ?? "medium",
        QuestionType = exam.Questions.FirstOrDefault()?.QuestionType.ToString() ?? "MCQ"
      };

      ViewData["JobName"] = exam.Job.Title;
      return View(examDetailsDto);
    }

    // POST: Exams/PreviewExam/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PreviewExam(int id, Dictionary<int, string> answers)
    {
      var exam = await _context.Exams
          .Include(e => e.Questions)
              .ThenInclude(q => q.Options)
          .FirstOrDefaultAsync(e => e.Id == id);

      if (exam == null)
      {
        return NotFound();
      }

      int correctAnswers = 0;
      int totalQuestions = exam.Questions.Count;

      foreach (var question in exam.Questions)
      {
        if (answers.TryGetValue(question.Id, out string? userAnswer))
        {
          bool isCorrect = false;

          switch (question.QuestionType)
          {
            case QuestionTypeEnum.MCQ:
              var selectedOption = question.Options.FirstOrDefault(o => o.Id.ToString() == userAnswer);
              isCorrect = selectedOption?.IsCorrect ?? false;
              break;

            case QuestionTypeEnum.TF:
              if (bool.TryParse(userAnswer, out bool boolAnswer))
              {
                isCorrect = boolAnswer == question.TrueFalseAnswer;
              }
              break;

            case QuestionTypeEnum.ShortAnswer:
            case QuestionTypeEnum.FillInTheBlank:
              isCorrect = string.Equals(question.Answer, userAnswer, StringComparison.OrdinalIgnoreCase);
              break;
          }

          if (isCorrect)
          {
            correctAnswers++;
          }
        }
      }

      double score = totalQuestions > 0 ? (double)correctAnswers / totalQuestions * 100 : 0;

      ViewData["Score"] = score;
      ViewData["CorrectAnswers"] = correctAnswers;
      ViewData["TotalQuestions"] = totalQuestions;

      return RedirectToAction(nameof(PreviewResults), new { id });
    }

    // GET: Exams/PreviewResults/5
    public async Task<IActionResult> PreviewResults(int? id)
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

      var examDetailsDto = new DTOs.ExamModels.ExamDetailsDTO
      {
        Id = exam.Id,
        Name = exam.Name,
        Description = exam.Description,
        JobId = exam.JobId,
        Duration = exam.Duration ?? 60,
        QuestionCount = exam.Questions.Count,
        Difficulty = exam.Questions.FirstOrDefault()?.QuestionSet?.Difficulty ?? "medium",
        QuestionType = exam.Questions.FirstOrDefault()?.QuestionType.ToString() ?? "MCQ"
      };

      ViewData["JobName"] = exam.Job.Title;
      return View(examDetailsDto);
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
      var exam = await _context.Exams
          .Include(e => e.ExamQuestionSets)
          .Include(e => e.Questions)
          .Include(e => e.CandidateExams)
          .FirstOrDefaultAsync(e => e.Id == id);

      if (exam != null)
      {
        // حذف العلاقات المرتبطة أولاً
        _context.ExamQuestionSets.RemoveRange(exam.ExamQuestionSets);
        _context.Questions.RemoveRange(exam.Questions);
        _context.CandidateExams.RemoveRange(exam.CandidateExams);

        _context.Exams.Remove(exam);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "تم حذف الاختبار بنجاح";
      }

      return RedirectToAction(nameof(Index));
    }

    // GET: Exams/QuestionSets/5
    public async Task<IActionResult> QuestionSets(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var exam = await _context.Exams
          .Include(e => e.ExamQuestionSets)
              .ThenInclude(eqs => eqs.QuestionSet)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (exam == null)
      {
        return NotFound();
      }

      var questionSets = exam.ExamQuestionSets.Select(eqs => new QuestionSetDto
      {
        Id = eqs.QuestionSet.Id,
        Name = eqs.QuestionSet.Name,
        Description = eqs.QuestionSet.Description,
        QuestionType = eqs.QuestionSet.QuestionType.ToString(),
        Language = eqs.QuestionSet.Language,
        QuestionCount = eqs.QuestionSet.QuestionCount,
        OptionsCount = eqs.QuestionSet.OptionsCount ?? 4,
        Difficulty = eqs.QuestionSet.Difficulty,
        Status = eqs.QuestionSet.Status.ToString(),
        CreatedDate = eqs.QuestionSet.CreatedAt,
        CompletedDate = eqs.QuestionSet.ProcessedAt
      }).ToList();

      ViewBag.ExamName = exam.Name;
      ViewBag.ExamId = exam.Id;

      return View(questionSets);
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
              .ThenInclude(ca => ca.Question)
                  .ThenInclude(q => q.Options)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (candidateExam == null)
      {
        return NotFound();
      }

      var resultDetail = new ExamResultDetailDto
      {
        Id = candidateExam.Id,
        ExamId = candidateExam.ExamId,
        ExamName = candidateExam.Exam.Name,
        ApplicantName = candidateExam.Candidate.Name,
        ApplicantEmail = candidateExam.Candidate.Email,
        StartTime = candidateExam.StartTime,
        EndTime = candidateExam.EndTime,
        Score = candidateExam.Score.HasValue ? (double)candidateExam.Score.Value : 0,
        TotalQuestions = candidateExam.CandidateAnswers.Count,
        CorrectAnswers = candidateExam.CandidateAnswers.Count(ca => ca.IsCorrect == true),
        IncorrectAnswers = candidateExam.CandidateAnswers.Count(ca => ca.IsCorrect == false),
        Answers = candidateExam.CandidateAnswers.Select(ca => new AnswerDetailDto
        {
          Id = ca.Id,
          QuestionId = ca.QuestionId,
          QuestionText = ca.Question.QuestionText,
          UserAnswer = GetFormattedAnswer(ca, ca.Question),
          CorrectAnswer = GetCorrectAnswer(ca.Question),
          IsCorrect = ca.IsCorrect ?? false,
          Explanation = ca.Question.SampleAnswer
        }).ToList()
      };

      return View(resultDetail);
    }

    // POST: Exams/GenerateQuestionsEnhanced
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateQuestionsEnhanced(int id, string sourceType,
        string? topic, string? textContent, string? linkUrl, string? youtubeUrl,
        QuestionTypeEnum questionType, string difficulty, int numberOfQuestions,
        string language, int? numberOfCorrectOptions)
    {
      var exam = await _context.Exams.FindAsync(id);
      if (exam == null)
      {
        return NotFound();
      }

      // Create a new question set
      var questionSet = new QuestionSet
      {
        Name = $"Generated Questions - {sourceType}",
        Description = $"Auto-generated from {sourceType}",
        QuestionType = questionType,
        Language = language,
        Difficulty = difficulty,
        QuestionCount = numberOfQuestions,
        OptionsCount = questionType == QuestionTypeEnum.MCQ ? 4 : null,
        NumberOfCorrectOptions = numberOfCorrectOptions,
        Status = QuestionSetStatus.Pending,
        CreatedAt = DateTime.UtcNow
      };

      _context.QuestionSets.Add(questionSet);
      await _context.SaveChangesAsync();

      // Create content source based on type
      var contentSource = new ContentSource
      {
        QuestionSetId = questionSet.Id,
        CreatedAt = DateTime.UtcNow
      };

      switch (sourceType.ToLower())
      {
        case "topic":
          contentSource.Type = ContentSourceType.Topic;
          contentSource.Content = topic;
          break;
        case "text":
          contentSource.Type = ContentSourceType.Text;
          contentSource.Content = textContent;
          break;
        case "link":
          contentSource.Type = ContentSourceType.Link;
          contentSource.Url = linkUrl;
          break;
        case "youtube":
          contentSource.Type = ContentSourceType.Youtube;
          contentSource.Url = youtubeUrl;
          break;
      }

      _context.ContentSources.Add(contentSource);
      await _context.SaveChangesAsync();

      // Link to exam
      var examQuestionSet = new ExamQuestionSet
      {
        ExamId = exam.Id,
        QuestionSetId = questionSet.Id,
        DisplayOrder = 1
      };

      _context.ExamQuestionSets.Add(examQuestionSet);
      await _context.SaveChangesAsync();

      // Generate questions asynchronously
      await _opExamsService.GenerateQuestionsAsync(questionSet.Id);

      TempData["SuccessMessage"] = "تم بدء عملية توليد الأسئلة بنجاح";
      return RedirectToAction(nameof(Details), new { id });
    }

    // POST: Exams/GenerateQuestionsFromFile
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateQuestionsFromFile(int id, IFormFile file,
        string language, int? numberOfCorrectOptions)
    {
      var exam = await _context.Exams.FindAsync(id);
      if (exam == null)
      {
        return NotFound();
      }

      if (file == null || file.Length == 0)
      {
        TempData["ErrorMessage"] = "يرجى اختيار ملف";
        return RedirectToAction(nameof(GenerateQuestions), new { id });
      }

      // Upload file
      using var stream = file.OpenReadStream();
      var fileId = await _opExamsService.UploadFile(stream, file.FileName);

      // Create question set
      var questionSet = new QuestionSet
      {
        Name = $"Questions from {file.FileName}",
        Description = $"Generated from uploaded file",
        QuestionType = QuestionTypeEnum.MCQ,
        Language = language,
        Difficulty = "medium",
        QuestionCount = 10,
        Status = QuestionSetStatus.Pending,
        CreatedAt = DateTime.UtcNow
      };

      _context.QuestionSets.Add(questionSet);
      await _context.SaveChangesAsync();

      // Create uploaded file record
      var uploadedFile = new UploadedFile
      {
        FileName = file.FileName,
        FileId = fileId,
        FileType = GetFileType(file.FileName),
        ContentType = file.ContentType,
        FileSize = file.Length,
        CreatedAt = DateTime.UtcNow
      };

      _context.UploadedFiles.Add(uploadedFile);
      await _context.SaveChangesAsync();

      // Create content source
      var contentSource = new ContentSource
      {
        Type = ContentSourceType.Document,
        UploadedFileId = uploadedFile.Id,
        QuestionSetId = questionSet.Id,
        CreatedAt = DateTime.UtcNow
      };

      _context.ContentSources.Add(contentSource);
      await _context.SaveChangesAsync();

      // Link to exam
      var examQuestionSet = new ExamQuestionSet
      {
        ExamId = exam.Id,
        QuestionSetId = questionSet.Id,
        DisplayOrder = 1
      };

      _context.ExamQuestionSets.Add(examQuestionSet);
      await _context.SaveChangesAsync();

      // Generate questions
      await _opExamsService.GenerateQuestionsAsync(questionSet.Id);

      TempData["SuccessMessage"] = "تم رفع الملف وبدأت عملية توليد الأسئلة";
      return RedirectToAction(nameof(Details), new { id });
    }

    // Helper methods
    private string GetFormattedAnswer(CandidateAnswer answer, Question question)
    {
      switch (question.QuestionType)
      {
        case QuestionTypeEnum.MCQ:
          var selectedOption = question.Options.FirstOrDefault(o => o.Id.ToString() == answer.AnswerText);
          return selectedOption?.Text ?? "لم يتم الإجابة";

        case QuestionTypeEnum.TF:
          return answer.AnswerText == "true" ? "صحيح" : "خطأ";

        default:
          return answer.AnswerText ?? "لم يتم الإجابة";
      }
    }

    private string GetCorrectAnswer(Question question)
    {
      switch (question.QuestionType)
      {
        case QuestionTypeEnum.MCQ:
          var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
          return correctOption?.Text ?? "غير متوفر";

        case QuestionTypeEnum.TF:
          return question.TrueFalseAnswer == true ? "صحيح" : "خطأ";

        default:
          return question.Answer ?? "غير متوفر";
      }
    }

    private FileType GetFileType(string fileName)
    {
      var extension = Path.GetExtension(fileName).ToLowerInvariant();
      return extension switch
      {
        ".pdf" or ".doc" or ".docx" => FileType.Document,
        ".jpg" or ".jpeg" or ".png" => FileType.Image,
        ".mp3" or ".wav" => FileType.Audio,
        ".mp4" or ".avi" => FileType.Video,
        _ => FileType.Other
      };
    }
  }
}
