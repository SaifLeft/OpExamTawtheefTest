using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TawtheefTest.Data.Structure;
using TawtheefTest.Enum;

namespace TawtheefTest.Services
{
  public class QuestionGenerationBackgroundService : BackgroundService
  {
    private readonly ILogger<QuestionGenerationBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public QuestionGenerationBackgroundService(
        ILogger<QuestionGenerationBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
      _logger = logger;
      _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      _logger.LogInformation("خدمة توليد الأسئلة في الخلفية بدأت العمل");

      while (!stoppingToken.IsCancellationRequested)
      {
        try
        {
          await ProcessPendingQuestionSets();
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "حدث خطأ أثناء معالجة مجموعات الأسئلة المعلقة");
        }

        // تنتظر لمدة دقيقة واحدة بين كل محاولة
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
      }
    }

    private async Task ProcessPendingQuestionSets()
    {
      using var scope = _serviceProvider.CreateScope();
      var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      var opExamsService = scope.ServiceProvider.GetRequiredService<IOpExamsService>();

      // البحث عن مجموعات الأسئلة التي بحالة "في الانتظار"
      var pendingQuestionSet = await context.QuestionSets
          .Where(qs => qs.Status == QuestionSetStatus.Pending)
          .OrderBy(qs => qs.CreatedAt)
          .FirstOrDefaultAsync();

      if (pendingQuestionSet == null)
      {
        return;
      }

      _logger.LogInformation("بدء معالجة مجموعة الأسئلة: {QuestionSetId}", pendingQuestionSet.Id);

      // تحديث الحالة إلى "قيد المعالجة"
      pendingQuestionSet.Status = QuestionSetStatus.Processing;
      await context.SaveChangesAsync();

      try
      {
        // استدعاء خدمة توليد الأسئلة
        bool success = await opExamsService.GenerateQuestionsAsync(pendingQuestionSet.Id);

        if (!success)
        {
          _logger.LogWarning("فشل توليد الأسئلة لمجموعة الأسئلة: {QuestionSetId}", pendingQuestionSet.Id);

          // تم تحديث الحالة بالفعل داخل GenerateQuestionsAsync
        }
        else
        {
          _logger.LogInformation("تم توليد الأسئلة بنجاح لمجموعة الأسئلة: {QuestionSetId}", pendingQuestionSet.Id);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "حدث خطأ أثناء توليد الأسئلة لمجموعة الأسئلة: {QuestionSetId}", pendingQuestionSet.Id);

        // تحديث الحالة إلى "فشل"
        pendingQuestionSet.Status = QuestionSetStatus.Failed;
        pendingQuestionSet.ErrorMessage = $"حدث خطأ غير متوقع: {ex.Message}";
        pendingQuestionSet.ProcessedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
      }
    }
  }
}
