using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using TawtheefTest.Enums;
using TawtheefTest.ViewModels;

namespace TawtheefTest.Services.Exams
{
  // IExamPublishingService.cs
  public interface IExamPublishingService
  {
    Task<PublishExamViewModel> PrepareExamForPublishingAsync(int examId);
    Task<(bool Success, string Message)> PublishExamAsync(PublishExamViewModel model);
    Task<bool> CanExamBePublishedAsync(int examId);
  }

  // ExamPublishingService.cs
  public class ExamPublishingService : IExamPublishingService
  {
    private readonly ApplicationDbContext _context;
    private readonly IExamValidationService _validationService;
    // private readonly ISmsService _smsService; // Uncomment when SMS service is available

    public ExamPublishingService(ApplicationDbContext context, IExamValidationService validationService)
    {
      _context = context;
      _validationService = validationService;
    }

    public async Task<PublishExamViewModel> PrepareExamForPublishingAsync(int examId)
    {
      var exam = await _context.Exams
          .Include(e => e.Job)
          .FirstOrDefaultAsync(m => m.Id == examId);

      if (exam == null) return null;

      var applicantsCount = await _context.Candidates
          .Where(c => c.JobId == exam.JobId && c.IsActive)
          .CountAsync();

      return new PublishExamViewModel
      {
        ExamId = exam.Id,
        ExamName = exam.Name,
        JobName = exam.Job.Title,
        StartDate = exam.StartDate,
        EndDate = exam.EndDate,
        SendSmsNotification = true,
        ApplicantsCount = applicantsCount
      };
    }

    public async Task<(bool Success, string Message)> PublishExamAsync(PublishExamViewModel model)
    {
      var exam = await _context.Exams
          .Include(e => e.Job)
          .Include(e => e.ExamQuestionSetMappings)
              .ThenInclude(eqs => eqs.QuestionSet)
                  .ThenInclude(qs => qs.Questions)
          .FirstOrDefaultAsync(e => e.Id == model.ExamId);

      if (exam == null)
        return (false, "الامتحان غير موجود");

      // Validate exam can be published
      var validationResult = await _validationService.ValidateExamForPublishingAsync(model.ExamId);
      if (!validationResult.IsValid)
        return (false, validationResult.ErrorMessage);

      // Update exam status
      exam.Status = nameof(ExamStatus.Published);
      exam.SendExamLinkToApplicants = true;
      exam.UpdatedAt = DateTime.UtcNow;

      _context.Update(exam);
      await _context.SaveChangesAsync();

      if (model.SendSmsNotification)
      {
        var notificationResult = await SendNotificationsToApplicantsAsync(exam, model);
        return (true, notificationResult);
      }

      return (true, "تم نشر الاختبار بنجاح");
    }

    public async Task<bool> CanExamBePublishedAsync(int examId)
    {
      var validationResult = await _validationService.ValidateExamForPublishingAsync(examId);
      return validationResult.IsValid;
    }

    private async Task<string> SendNotificationsToApplicantsAsync(Exam exam, PublishExamViewModel model)
    {
      var applicants = await _context.Candidates
          .Where(c => c.JobId == exam.JobId && c.IsActive)
          .ToListAsync();

      string messageTemplate = model.NotificationText ??
          $"مرحباً {{اسم_المتقدم}}، لديك اختبار \"{exam.Name}\" متاح من {{تاريخ_البدء}} إلى {{تاريخ_الانتهاء}}. " +
          $"يمكنك إجراء الاختبار في أي وقت خلال هذه الفترة. رابط الاختبار: {{رابط_الاختبار}}";

      string startDateStr = exam.StartDate.ToString("yyyy/MM/dd") ?? DateTime.Now.ToString("yyyy/MM/dd");
      string endDateStr = exam.EndDate.ToString("yyyy/MM/dd") ?? DateTime.Now.AddDays(7).ToString("yyyy/MM/dd");
      string examUrl = $"https://yoursite.com/CandidateExam/Start/{exam.Id}";

      int successCount = 0;
      int failedCount = 0;

      foreach (var applicant in applicants)
      {
        try
        {
          string personalizedMessage = messageTemplate
              .Replace("{اسم_المتقدم}", applicant.Name)
              .Replace("{اسم_الاختبار}", exam.Name)
              .Replace("{تاريخ_البدء}", startDateStr)
              .Replace("{تاريخ_الانتهاء}", endDateStr)
              .Replace("{رابط_الاختبار}", examUrl);

          // TODO: Implement actual SMS sending
          // await _smsService.SendSmsAsync(applicant.Phone, personalizedMessage);

          successCount++;
        }
        catch (Exception)
        {
          failedCount++;
        }
      }

      string resultMessage = $"تم نشر الاختبار بنجاح وإرسال {successCount} رسالة نصية للمتقدمين";
      if (failedCount > 0)
      {
        resultMessage += $"، وفشل إرسال {failedCount} رسالة";
      }

      return resultMessage;
    }
  }
}
