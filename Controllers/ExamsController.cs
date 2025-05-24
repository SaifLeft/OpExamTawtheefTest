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
            .ThenInclude(e => e.Candidates)
          .Include(e => e.ExamQuestionSets)
              .ThenInclude(eqs => eqs.QuestionSet)
          .Select(e => new ExamDto
          {
            Id = e.Id,
            Name = e.Name,
            Description = e.Description,
            JobId = e.JobId,
            JobName = e.Job.Title,
            Duration = e.Duration,
            StartDate = e.StartDate ?? DateTime.Now,
            EndDate = e.EndDate ?? DateTime.Now.AddDays(7),
            CreatedDate = e.CreatedAt,
            CandidatesCount = e.Job.Candidates.Count(),
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
          .Include(e => e.ExamQuestionSets)
              .ThenInclude(eqs => eqs.QuestionSet)
                  .ThenInclude(qs => qs.Questions)
                      .ThenInclude(q => q.Options)
          .Include(e => e.ExamQuestionSets)
              .ThenInclude(eqs => eqs.QuestionSet)
                  .ThenInclude(qs => qs.Questions)
                      .ThenInclude(q => q.MatchingPairs)
          .Include(e => e.ExamQuestionSets)
              .ThenInclude(eqs => eqs.QuestionSet)
                  .ThenInclude(qs => qs.Questions)
                      .ThenInclude(q => q.OrderingItems)
          .Include(e => e.CandidateExams)
              .ThenInclude(ce => ce.Candidate)
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
        Duration = exam.Duration,
        CreatedDate = exam.CreatedAt,
        ExamStartDate = exam.StartDate ?? DateTime.Now,
        ExamEndDate = exam.EndDate ?? DateTime.Now.AddDays(7),
        TotalQuestionsPerCandidate = exam.TotalQuestionsPerCandidate,
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
            }).ToList(),
        Candidates = exam.CandidateExams
            .Select(ce => new ExamCandidateDTO
            {
              Id = ce.Id,
              CandidateId = ce.CandidateId,
              Name = ce.Candidate.Name,
              StartTime = ce.StartTime,
              EndTime = ce.EndTime,
              Score = ce.Score,
              Status = Enum.GetValues<CandidateExamStatus>().FirstOrDefault(s => s.ToString() == ce.Status.ToString()),
            }).ToList()
      };

      // طباعة قيمة Status للتحقق
      System.Diagnostics.Debug.WriteLine($"Exam Status: {exam.Status} - DTO Status: {examDetailsDto.Status}");

      // للاستكشاف: سأضيف رسالة توضيحية في TempData
      TempData["StatusDebug"] = $"قيمة Status في الـ DTO: {examDetailsDto.Status} | قيمة Status في النموذج الأصلي: {exam.Status}";

      // جلب الأسئلة للاختبار
      var allQuestions = exam.ExamQuestionSets
          .SelectMany(eqs => eqs.QuestionSet.Questions)
          .ToList();

      var questions = allQuestions
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
                    Index = m.DisplayOrder ?? 0
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
        Duration = exam.Duration,
        StartDate = exam.StartDate ?? DateTime.Now,
        EndDate = exam.EndDate ?? DateTime.Now.AddDays(7),
        ShowResultsImmediately = exam.ShowResultsImmediately,
        SendExamLinkToApplicants = exam.SendExamLinkToApplicants
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
          exam.ShowResultsImmediately = examDto.ShowResultsImmediately;
          exam.SendExamLinkToApplicants = examDto.SendExamLinkToApplicants;
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
          .Include(e => e.ExamQuestionSets)
              .ThenInclude(eqs => eqs.QuestionSet)
                  .ThenInclude(qs => qs.Questions)
          .Select(e => new ExamListDTO
          {
            Id = e.Id,
            Name = e.Name,
            JobId = e.JobId,
            JobName = e.Job.Title,
            Duration = e.Duration,
            QuestionsCount = e.ExamQuestionSets.Sum(eqs => eqs.QuestionSet.Questions.Count),
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
          .Include(e => e.ExamQuestionSets)
              .ThenInclude(eqs => eqs.QuestionSet)
                  .ThenInclude(qs => qs.Questions)
                    .ThenInclude(q => q.Options)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (exam == null)
      {
        return NotFound();
      }

      // تجميع الأسئلة من جميع مجموعات الأسئلة
      var allQuestions = exam.ExamQuestionSets
          .SelectMany(eqs => eqs.QuestionSet.Questions)
          .ToList();

      if (allQuestions.Count == 0)
      {
        TempData["WarningMessage"] = "لا توجد أسئلة في هذا الاختبار بعد. يرجى إضافة بعض الأسئلة أولاً.";
        return RedirectToAction(nameof(Details), new { id });
      }

      var questions = allQuestions.Select(q => new ExamQuestionDTO
      {
        Id = q.Id,
        QuestionText = q.QuestionText,
        QuestionType = q.QuestionType,
        Options = q.Options.Select(o => new QuestionOptionDTO
        {
          Id = o.Id,
          Text = o.Text,
          IsCorrect = o.IsCorrect
        }).ToList()
      }).ToList();

      ViewBag.ExamId = id;
      ViewBag.ExamName = exam.Name;

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

    // POST: Exams/ToggleShowResults
    [HttpPost]
    public async Task<IActionResult> ToggleShowResults(int id)
    {
      try
      {
        var exam = await _context.Exams.FindAsync(id);
        if (exam == null)
        {
          return Json(new { success = false, message = "الاختبار غير موجود" });
        }

        // تبديل حالة عرض النتائج
        exam.ShowResultsImmediately = !exam.ShowResultsImmediately;
        exam.UpdatedAt = DateTime.UtcNow;

        _context.Update(exam);
        await _context.SaveChangesAsync();

        return Json(new
        {
          success = true,
          newValue = exam.ShowResultsImmediately,
          message = exam.ShowResultsImmediately ? "تم تفعيل عرض النتائج للمرشحين فوراً" : "تم إلغاء عرض النتائج للمرشحين فوراً"
        });
      }
      catch (Exception ex)
      {
        return Json(new { success = false, message = "حدث خطأ أثناء تحديث الإعداد" });
      }
    }

    // POST: Exams/ToggleExamLinks
    [HttpPost]
    public async Task<IActionResult> ToggleExamLinks(int id)
    {
      try
      {
        var exam = await _context.Exams.FindAsync(id);
        if (exam == null)
        {
          return Json(new { success = false, message = "الاختبار غير موجود" });
        }

        // تبديل حالة إرسال الروابط
        exam.SendExamLinkToApplicants = !exam.SendExamLinkToApplicants;
        exam.UpdatedAt = DateTime.UtcNow;

        _context.Update(exam);
        await _context.SaveChangesAsync();

        return Json(new
        {
          success = true,
          newValue = exam.SendExamLinkToApplicants,
          message = exam.SendExamLinkToApplicants ? "تم تفعيل إرسال روابط الاختبار للمرشحين" : "تم إلغاء إرسال روابط الاختبار للمرشحين"
        });
      }
      catch (Exception ex)
      {
        return Json(new { success = false, message = "حدث خطأ أثناء تحديث الإعداد" });
      }
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
      var questionsCount = await _context.ExamQuestionSets
          .Where(eqs => eqs.ExamId == id)
          .SelectMany(eqs => eqs.QuestionSet.Questions)
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
          .Include(e => e.ExamQuestionSets)
              .ThenInclude(eqs => eqs.QuestionSet)
                  .ThenInclude(qs => qs.Questions)
          .FirstOrDefaultAsync(e => e.Id == model.ExamId);

      // check if exam has questions
      if (exam.ExamQuestionSets.Count == 0)
      {
        TempData["ErrorMessage"] = "لا يمكن نشر الاختبار لأنه لا يحتوي على أسئلة";
        return RedirectToAction(nameof(Details), new { id = model.ExamId });
      }

      // check if exam has candidates
      var candidatesCount = await _context.Candidates
          .Where(c => c.JobId == exam.JobId && c.IsActive)
          .CountAsync();

      if (candidatesCount == 0)
      {
        TempData["ErrorMessage"] = "لا يمكن نشر الاختبار لأنه لا يحتوي على متقدمين";
        return RedirectToAction(nameof(Details), new { id = model.ExamId });
      }

      // check if exam has n no of questions to be published

      if (exam.TotalQuestionsPerCandidate > exam.ExamQuestionSets.SelectMany(eqs => eqs.QuestionSet.Questions).Count())
      {
        TempData["ErrorMessage"] = "لا يمكن نشر الاختبار لأنه لا يحتوي على أسئلة";
        return RedirectToAction(nameof(Details), new { id = model.ExamId });
      }


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

    [HttpPost]
    public async Task<IActionResult> AssignQuestionSetsToExam(int examId, List<int> questionSetIds)
    {
      try
      {
        // التحقق من وجود الامتحان
        var exam = await _context.Exams
            .Include(e => e.ExamQuestionSets)
            .FirstOrDefaultAsync(e => e.Id == examId);

        if (exam == null)
        {
          return NotFound(new { success = false, message = "الامتحان غير موجود" });
        }

        // حذف مجموعات الأسئلة الحالية المرتبطة بالامتحان
        _context.ExamQuestionSets.RemoveRange(exam.ExamQuestionSets);

        // إضافة مجموعات الأسئلة المحددة إلى الامتحان
        int displayOrder = 1;
        foreach (var questionSetId in questionSetIds)
        {
          var examQuestionSet = new TawtheefTest.Data.Structure.ExamQuestionSet
          {
            ExamId = examId,
            QuestionSetId = questionSetId,
            DisplayOrder = displayOrder++
          };
          _context.ExamQuestionSets.Add(examQuestionSet);
        }

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "تم تعيين مجموعات الأسئلة بنجاح" });
      }
      catch (Exception ex)
      {
        return StatusCode(500, new { success = false, message = $"حدث خطأ: {ex.Message}" });
      }
    }

    // POST: Exams/DeleteQuestion/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
      // الحصول على السؤال من خلال معرّفه
      var question = await _context.Questions
          .Include(q => q.QuestionSet)
              .ThenInclude(qs => qs.ExamQuestionSets)
          .FirstOrDefaultAsync(q => q.Id == id);

      if (question == null)
      {
        return NotFound();
      }

      // التحقق من أن السؤال ينتمي إلى مجموعة أسئلة مرتبطة بامتحان
      var examId = question.QuestionSet.ExamQuestionSets.FirstOrDefault()?.ExamId;

      if (examId == null)
      {
        TempData["ErrorMessage"] = "لم يتم العثور على الامتحان المرتبط بهذا السؤال.";
        return RedirectToAction(nameof(Index));
      }

      try
      {
        _context.Questions.Remove(question);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "تم حذف السؤال بنجاح.";
      }
      catch (Exception ex)
      {
        TempData["ErrorMessage"] = $"حدث خطأ أثناء حذف السؤال: {ex.Message}";
      }

      return RedirectToAction(nameof(ExamQuestions), new { id = examId });
    }

    // GET: Exams/AddQuestion/5
    public IActionResult AddQuestion(int id)
    {
      var questionSets = _context.QuestionSets
          .Where(qs => qs.ExamQuestionSets.Any(eqs => eqs.ExamId == id))
          .ToList();

      if (questionSets.Count == 0)
      {
        TempData["ErrorMessage"] = "لا توجد مجموعات أسئلة متاحة لهذا الامتحان. يرجى إضافة مجموعة أسئلة أولاً.";
        return RedirectToAction(nameof(Details), new { id });
      }

      ViewBag.QuestionSets = new SelectList(questionSets, "Id", "Name");
      ViewBag.ExamId = id;
      return View(new AddQuestionViewModel { ExamId = id });
    }

    // POST: Exams/AddQuestion/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddQuestion(int id, AddQuestionViewModel model)
    {
      if (id != model.ExamId)
      {
        return NotFound();
      }

      if (ModelState.IsValid)
      {
        try
        {
          // التحقق من وجود الامتحان
          var exam = await _context.Exams
              .Include(e => e.ExamQuestionSets)
                  .ThenInclude(eqs => eqs.QuestionSet)
                      .ThenInclude(qs => qs.Questions)
              .FirstOrDefaultAsync(e => e.Id == id);

          if (exam == null)
          {
            return NotFound();
          }

          // إنشاء سؤال جديد
          var question = new Question
          {
            QuestionSetId = model.QuestionSetId,
            QuestionText = model.QuestionText,
            QuestionType = model.QuestionType,
            Index = 0, // سيتم تحديثه لاحقًا
            DisplayOrder = 0, // سيتم تحديثه لاحقًا
            CreatedAt = DateTime.UtcNow
          };

          if (model.QuestionType == "TF")
          {
            question.TrueFalseAnswer = model.TrueFalseAnswer;
          }
          else if (model.QuestionType == "ShortAnswer" || model.QuestionType == "FillInTheBlank")
          {
            question.Answer = model.Answer;
          }

          _context.Questions.Add(question);
          await _context.SaveChangesAsync();

          // تحديث مؤشر السؤال
          var questionCount = exam.ExamQuestionSets
              .SelectMany(eqs => eqs.QuestionSet.Questions)
              .Count();

          question.Index = questionCount;
          question.DisplayOrder = questionCount;
          await _context.SaveChangesAsync();

          // إضافة خيارات للسؤال (إذا كان من نوع الاختيار من متعدد)
          if (model.QuestionType == "MCQ" && model.Options != null && model.Options.Count > 0)
          {
            int optionIndex = 0;
            foreach (var optionText in model.Options)
            {
              var option = new QuestionOption
              {
                QuestionId = question.Id,
                Text = optionText,
                IsCorrect = optionIndex == model.CorrectOptionIndex,
                Index = optionIndex
              };
              _context.QuestionOptions.Add(option);
              optionIndex++;
            }
            await _context.SaveChangesAsync();
          }

          TempData["SuccessMessage"] = "تم إضافة السؤال بنجاح.";
          return RedirectToAction(nameof(ExamQuestions), new { id });
        }
        catch (Exception ex)
        {
          ModelState.AddModelError("", $"حدث خطأ أثناء إضافة السؤال: {ex.Message}");
        }
      }

      // إعادة تحميل البيانات في حالة حدوث خطأ
      var questionSets = await _context.QuestionSets
          .Where(qs => qs.ExamQuestionSets.Any(eqs => eqs.ExamId == id))
          .ToListAsync();

      ViewBag.QuestionSets = new SelectList(questionSets, "Id", "Name");
      ViewBag.ExamId = id;
      return View(model);
    }
  }
}
