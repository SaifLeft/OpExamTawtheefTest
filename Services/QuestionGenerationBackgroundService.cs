using ITAM.Service;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using TawtheefTest.DTOs.OpExam;
using TawtheefTest.Enums;

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
          await ProcessUnProcessedWithErrors(); // process question sets with status failed
          await GetFileUploadedCodeFromOpExams();
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
      var opExamsService = scope.ServiceProvider.GetRequiredService<IOpExamQuestionGenerationService>();

      // البحث عن مجموعات الأسئلة التي بحالة "في الانتظار"
      var pendingQuestionSet = await context.QuestionSets
          .Where(qs => qs.Status == nameof(QuestionSetStatus.Pending))
          .OrderBy(qs => qs.CreatedAt)
          .FirstOrDefaultAsync();

      if (pendingQuestionSet == null)
      {
        return;
      }

      _logger.LogInformation("بدء معالجة مجموعة الأسئلة: {QuestionSetId}", pendingQuestionSet.Id);

      // تحديث الحالة إلى "قيد المعالجة"
      pendingQuestionSet.Status = nameof(QuestionSetStatus.Processing);
      pendingQuestionSet.UpdatedAt = DateTime.Now;
      pendingQuestionSet.RetryCount = pendingQuestionSet.RetryCount + 1;
      await context.SaveChangesAsync();

      try
      {
        OpExamQuestionGenerationRequest request = new OpExamQuestionGenerationRequest();
        ContentSourceType contentType = Enum.Parse<ContentSourceType>(pendingQuestionSet.ContentSourceType, true);
        request.QuestionType = pendingQuestionSet.QuestionType;
        request.Language = pendingQuestionSet.Language;
        request.NumberOfOptions = pendingQuestionSet.OptionsCount ?? 4;
        request.NumberOfQuestions = pendingQuestionSet.QuestionCount;
        request.Difficulty = pendingQuestionSet.DifficultySet;
        request.NumberOfRows = pendingQuestionSet.NumberOfRows;
        request.NumberOfCorrectOptions = pendingQuestionSet.NumberOfCorrectOptions?.ToString();

        request.SourceContent = new SourceContent();
        switch (contentType)
        {
          case ContentSourceType.Topic:
            request.SourceContent.Type = nameof(ContentSourceType.Topic).ToLower();
            request.SourceContent.Topic = pendingQuestionSet.Content ?? throw new Exception("لا يوجد محتوى للمحتوى");
            break;
          case ContentSourceType.Text:
            request.SourceContent.Type = nameof(ContentSourceType.Text).ToLower();
            request.SourceContent.Text = pendingQuestionSet.Content ?? throw new Exception("لا يوجد محتوى للمحتوى");
            break;
          case ContentSourceType.Link:
            request.SourceContent.Type = nameof(ContentSourceType.Link).ToLower();
            request.SourceContent.Link = pendingQuestionSet.Url ?? throw new Exception("لا يوجد رابط للمحتوى");
            break;
          case ContentSourceType.Youtube:
            request.SourceContent.Type = nameof(ContentSourceType.Youtube).ToLower();
            request.SourceContent.Link = pendingQuestionSet.Url ?? throw new Exception("لا يوجد رابط للمحتوى");
            break;
          case ContentSourceType.Document:

            request.SourceContent.Type = "pdf";
            request.SourceContent.Document = pendingQuestionSet.FileUploadedCode ?? throw new Exception("لا يوجد رمز للمحتوى");
            break;
          case ContentSourceType.Image:
            request.SourceContent.Type = nameof(ContentSourceType.Image).ToLower();
            request.SourceContent.Image = pendingQuestionSet.FileUploadedCode ?? throw new Exception("لا يوجد رابط للمحتوى");
            break;
          case ContentSourceType.Audio:
            request.SourceContent.Type = nameof(ContentSourceType.Audio).ToLower();
            request.SourceContent.Audio = pendingQuestionSet.FileUploadedCode ?? throw new Exception("لا يوجد رابط للمحتوى");
            break;
          case ContentSourceType.Video:
            request.SourceContent.Type = nameof(ContentSourceType.Video).ToLower();
            request.SourceContent.Video = pendingQuestionSet.FileUploadedCode ?? throw new Exception("لا يوجد رابط للمحتوى");
            break;
          default:
            throw new Exception("نوع المحتوى غير معرف");
        }

        await opExamsService.GenerateQuestionsAsync(pendingQuestionSet.Id, request);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "حدث خطأ أثناء توليد الأسئلة لمجموعة الأسئلة: {QuestionSetId}", pendingQuestionSet.Id);

        // تحديث الحالة إلى "فشل"
        pendingQuestionSet.Status = nameof(QuestionSetStatus.Failed);
        pendingQuestionSet.RetryCount = pendingQuestionSet.RetryCount + 1;
        pendingQuestionSet.ErrorMessage = $"حدث خطأ غير متوقع: {ex.Message}";
        pendingQuestionSet.UpdatedAt = DateTime.Now;
        await context.SaveChangesAsync();
      }
    }
    private async Task GetFileUploadedCodeFromOpExams()
    {
      using var scope = _serviceProvider.CreateScope();
      var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      var opExamsService = scope.ServiceProvider.GetRequiredService<IOpExamQuestionGenerationService>();
      var fileManagerService = scope.ServiceProvider.GetRequiredService<IFileMangmanent>();
      // get question sets with file filename not null and file uploaded code is null
      var questionSets = await context.QuestionSets
          .Where(qs => qs.FileUploadedCode == null && qs.FileName != null)
          .ToListAsync();

      foreach (var questionSet in questionSets)
      {
        ContentSourceType contentType = Enum.Parse<ContentSourceType>(questionSet.ContentSourceType, true);
        var fileResponse = fileManagerService.GetFileByName(questionSet.FileName, contentType);
        if (fileResponse.IsSuccess)
        {
          var IninsietUploadToOpExam = await opExamsService.UploadFileAsync(fileResponse.FileBytes, questionSet.FileName);
          if (IninsietUploadToOpExam != null)
          {
            questionSet.FileUploadedCode = IninsietUploadToOpExam;
            await context.SaveChangesAsync();
          }
        }
      }

    }
    private async Task ProcessUnProcessedWithErrors()
    {
      using var scope = _serviceProvider.CreateScope();
      var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      var opExamsService = scope.ServiceProvider.GetRequiredService<IOpExamQuestionGenerationService>();

      var questionSets = await context.QuestionSets
          .Where(qs => (qs.Status == nameof(QuestionSetStatus.Processing) || qs.Status == nameof(QuestionSetStatus.Failed)) && qs.UpdatedAt != null && qs.UpdatedAt < DateTime.Now.AddMinutes(-3))
          .ToListAsync();

      foreach (var questionSet in questionSets)
      {
        questionSet.Status = nameof(QuestionSetStatus.Pending);
        questionSet.UpdatedAt = DateTime.Now;
      }

      await context.SaveChangesAsync();
    }
  }
}
