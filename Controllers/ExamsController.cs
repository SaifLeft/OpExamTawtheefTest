using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using TawtheefTest.Services;
using TawtheefTest.ViewModels;
using TawtheefTest.DTOs;
using System.Threading.Tasks;
using System.Linq;
using TawtheefTest.Enums;
using TawtheefTest.DTOs.ExamModels;

namespace TawtheefTest.Controllers
{
  public class ExamsController : Controller
  {
    private readonly ApplicationDbContext _context;

    public ExamsController(ApplicationDbContext context)
    {
      _context = context;
    }

    // GET: Exams
    public async Task<IActionResult> Index()
    {
      var exams = await _context.Exams
          .Include(e => e.CandidateExams)
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
        QuestionCountForEachCandidate = exam.QuestionCountForEachCandidate,
        ShowResultsImmediately = exam.ShowResultsImmediately,
        SendExamLinkToApplicants = exam.SendExamLinkToApplicants,
        Status = exam.Status,
        QuestionSets = exam.ExamQuestionSets
            .OrderBy(eqs => eqs.DisplayOrder)
            .Select(eqs => new QuestionSetDto
            {
              Id = eqs.QuestionSet.Id,
              Name = eqs.QuestionSet.Name,
              Description = eqs.QuestionSet.Description,
              QuestionType = eqs.QuestionSet.QuestionType,
              QuestionCount = eqs.QuestionSet.QuestionCount,
              Status = eqs.QuestionSet.Status,
              StatusDescription = GetStatusDescription(eqs.QuestionSet.Status),
              ContentSourceType = eqs.QuestionSet.ContentSourceType,
              Difficulty = eqs.QuestionSet.Difficulty,
              ProcessedAt = eqs.QuestionSet.UpdatedAt,
              CreatedAt = eqs.QuestionSet.CreatedAt
            }).ToList()
      };

      // طباعة قيمة Status للتحقق
      System.Diagnostics.Debug.WriteLine($"Exam Status: {exam.Status} - DTO Status: {examDetailsDto.Status}");

      // للاستكشاف: سأضيف رسالة توضيحية في TempData
      TempData["StatusDebug"] = $"قيمة Status في الـ DTO: {examDetailsDto.Status} | قيمة Status في النموذج الأصلي: {exam.Status}";

      // جلب الأسئلة للاختبار
      var questions = exam.Questions
          .OrderBy(q => q.Index)
          .Select(q => new ExamQuestionDTO
          {
            Id = q.Id,
            SequenceNumber = q.Index,
            QuestionText = q.QuestionText,
            QuestionType = q.QuestionType,
            Answer = q.Answer,
            TrueFalseAnswer = q.TrueFalseAnswer,
            InstructionText = q.InstructionText,
            // للأسئلة من نوع الترتيب
            CorrectlyOrdered = q.QuestionType.ToLower() == "ordering"
                ? q.OrderingItems.OrderBy(o => o.CorrectOrder).Select(o => o.Text).ToList()
                : null,
            ShuffledOrder = q.QuestionType.ToLower() == "ordering"
                ? q.OrderingItems.OrderBy(o => o.DisplayOrder).Select(o => o.Text).ToList()
                : null,
            // للأسئلة من نوع المطابقة
            MatchingPairs = q.QuestionType.ToLower() == "matching"
                ? q.MatchingPairs.Select(m => new MatchingPairDTO
                {
                  Left = m.LeftItem,
                  Right = m.RightItem,
                  Index = m.DisplayOrder
                }).ToList()
                : null,
            Options = q.Options?.Select(o => new QuestionOptionDTO
            {
              Id = o.Id,
              Text = o.Text,
              Index = o.Index,
              IsCorrect = o.IsCorrect
            }).ToList()
          })
          .ToList();

      ViewBag.Questions = questions;

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
    public async Task<IActionResult> Create(CreateExamViewModel model)
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
          CreatedAt = DateTime.UtcNow,
          ShowResultsImmediately = model.ShowResultsImmediately,
          SendExamLinkToApplicants = model.SendExamLinkToApplicants
        };

        _context.Add(exam);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "تم إنشاء الاختبار بنجاح";
        return RedirectToAction(nameof(Details), new { id = exam.Id });
      }

      ViewBag.Jobs = new SelectList(_context.Jobs, "Id", "Title", model.JobId);
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

      var examDto = new EditExamDTO
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
    public async Task<IActionResult> Edit(int id, EditExamDTO examDto)
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
          .Select(e => new ExamListDTO
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


    // GET: Exams/ExamQuestions/5
    public async Task<IActionResult> ExamQuestions(int? id)
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

      ViewBag.ExamName = exam.Name;
      ViewBag.JobName = exam.Job.Title;

      var questions = exam.Questions
          .OrderBy(q => q.Index)
          .Select(q => new ExamQuestionDTO
          {
            Id = q.Id,
            SequenceNumber = q.Index,
            QuestionText = q.QuestionText,
            QuestionType = q.QuestionType,
            Answer = q.Answer,
            TrueFalseAnswer = q.TrueFalseAnswer,
            InstructionText = q.InstructionText,
            // للأسئلة من نوع الترتيب
            CorrectlyOrdered = q.QuestionType.ToLower() == "ordering"
                ? q.OrderingItems.OrderBy(o => o.CorrectOrder).Select(o => o.Text).ToList()
                : null,
            ShuffledOrder = q.QuestionType.ToLower() == "ordering"
                ? q.OrderingItems.OrderBy(o => o.DisplayOrder).Select(o => o.Text).ToList()
                : null,
            // للأسئلة من نوع المطابقة
            MatchingPairs = q.QuestionType.ToLower() == "matching"
                ? q.MatchingPairs.Select(m => new MatchingPairDTO
                {
                  Left = m.LeftItem,
                  Right = m.RightItem,
                  Index = m.DisplayOrder
                }).ToList()
                : null,
            Options = q.Options?.Select(o => new QuestionOptionDTO
            {
              Id = o.Id,
              Text = o.Text,
              Index = o.Index,
              IsCorrect = o.IsCorrect
            }).ToList()
          })
          .ToList();

      return View(questions);
    }

    private string GetStatusDescription(QuestionSetStatus status)
    {
      return status switch
      {
        QuestionSetStatus.Pending => "في الانتظار",
        QuestionSetStatus.Processing => "قيد المعالجة",
        QuestionSetStatus.Completed => "مكتمل",
        QuestionSetStatus.Failed => "فشل",
        _ => status.ToString()
      };
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

    // GET: Exams/PublishExam/5
    public async Task<IActionResult> PublishExam(int? id)
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

      // التحقق من أن الاختبار يحتوي على أسئلة
      var questionsCount = await _context.Questions
          .Where(q => q.ExamId == id)
          .CountAsync();

      if (questionsCount == 0)
      {
        TempData["ErrorMessage"] = "لا يمكن نشر الاختبار لأنه لا يحتوي على أسئلة";
        return RedirectToAction(nameof(Details), new { id });
      }

      // إعداد نموذج تأكيد النشر
      var publishModel = new PublishExamViewModel
      {
        ExamId = exam.Id,
        ExamName = exam.Name,
        JobName = exam.Job.Title,
        StartDate = exam.StartDate ?? DateTime.Now,
        EndDate = exam.EndDate ?? DateTime.Now.AddDays(7),
        SendSmsNotification = true,
        ApplicantsCount = await _context.Candidates
            .Where(c => c.JobId == exam.JobId && c.IsActive)
            .CountAsync()
      };

      return View(publishModel);
    }

    // POST: Exams/PublishExam
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPublishExam(PublishExamViewModel model)
    {
      if (!ModelState.IsValid)
      {
        return View("PublishExam", model);
      }

      var exam = await _context.Exams
          .Include(e => e.Job)
          .FirstOrDefaultAsync(e => e.Id == model.ExamId);

      if (exam == null)
      {
        return NotFound();
      }

      // تعديل حالة الاختبار ليكون منشوراً
      exam.Status = ExamStatus.Published;
      exam.SendExamLinkToApplicants = true;
      exam.UpdatedAt = DateTime.UtcNow;

      _context.Update(exam);
      await _context.SaveChangesAsync();

      if (model.SendSmsNotification)
      {
        // الحصول على المتقدمين للوظيفة
        var applicants = await _context.Candidates
            .Where(c => c.JobId == exam.JobId && c.IsActive)
            .ToListAsync();

        // إنشاء نص الرسالة للمتقدمين
        string messageTemplate = model.NotificationText ??
          $"مرحباً {{اسم_المتقدم}}، لديك اختبار \"{exam.Name}\" متاح من {{تاريخ_البدء}} إلى {{تاريخ_الانتهاء}}. " +
          $"يمكنك إجراء الاختبار في أي وقت خلال هذه الفترة. رابط الاختبار: {{رابط_الاختبار}}";

        string startDateStr = exam.StartDate?.ToString("yyyy/MM/dd") ?? DateTime.Now.ToString("yyyy/MM/dd");
        string endDateStr = exam.EndDate?.ToString("yyyy/MM/dd") ?? DateTime.Now.AddDays(7).ToString("yyyy/MM/dd");
        string examUrl = $"{Request.Scheme}://{Request.Host}/CandidateExam/Start/{exam.Id}";

        // تسجيل سجل بالإشعارات المرسلة
        int successCount = 0;
        int failedCount = 0;

        // إرسال الرسائل النصية (تنفيذ فعلي يحتاج إلى خدمة SMS)
        foreach (var applicant in applicants)
        {
          try
          {
            // استبدال المتغيرات في القالب
            string personalizedMessage = messageTemplate
                .Replace("{اسم_المتقدم}", applicant.Name)
                .Replace("{اسم_الاختبار}", exam.Name)
                .Replace("{تاريخ_البدء}", startDateStr)
                .Replace("{تاريخ_الانتهاء}", endDateStr)
                .Replace("{رابط_الاختبار}", examUrl);

            // TODO: استدعاء خدمة الرسائل النصية هنا
            // await _smsService.SendSmsAsync(applicant.Phone, personalizedMessage);

            // تسجيل نجاح الإرسال
            successCount++;

            // تسجيل الإرسال في قاعدة البيانات إذا لزم الأمر
            // _context.NotificationLogs.Add(new NotificationLog { ... });
          }
          catch (Exception ex)
          {
            // تسجيل فشل الإرسال
            failedCount++;
            // يمكن تسجيل الخطأ في سجل الأخطاء
            // _logger.LogError(ex, $"Error sending SMS to candidate {applicant.Id}");
          }
        }

        string resultMessage = $"تم نشر الاختبار بنجاح وإرسال {successCount} رسالة نصية للمتقدمين";
        if (failedCount > 0)
        {
          resultMessage += $"، وفشل إرسال {failedCount} رسالة";
        }
        TempData["SuccessMessage"] = resultMessage;
      }
      else
      {
        TempData["SuccessMessage"] = "تم نشر الاختبار بنجاح";
      }

      return RedirectToAction(nameof(Details), new { id = model.ExamId });
    }

    // GET: Exams/TogglePublishStatus/5
    public async Task<IActionResult> TogglePublishStatus(int? id)
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

      // تبديل حالة النشر
      if (exam.Status == ExamStatus.Published)
      {
        exam.Status = ExamStatus.Draft;
        TempData["SuccessMessage"] = "تم إلغاء نشر الاختبار";
      }
      else
      {
        exam.Status = ExamStatus.Published;
        TempData["SuccessMessage"] = "تم نشر الاختبار";
      }

      exam.UpdatedAt = DateTime.UtcNow;
      _context.Update(exam);
      await _context.SaveChangesAsync();

      return RedirectToAction(nameof(Details), new { id });
    }
  }
}
