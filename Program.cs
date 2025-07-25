using ITAM.Service;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using TawtheefTest.Infrastructure;
using TawtheefTest.Services;
using TawtheefTest.Services.Exams;
using TawtheefTest.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// Add layout template data
builder.Services.AddSingleton<IStartupFilter, LayoutDataInitializer>();

// Database configuration - using SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Register services
builder.Services.AddHttpClient();
builder.Services.AddScoped<IOTPService, OTPService>();
builder.Services.AddTransient<IFileMangmanent, FileMangmanent>();
builder.Services.AddTransient<IOpExamQuestionGenerationService, OpExamQuestionGenerationService>();
builder.Services.AddTransient<IQuestionSetLibraryService, QuestionSetLibraryService>();
builder.Services.AddScoped<IQuestionGenerationService, QuestionGenerationService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ExamEvaluationService>();


builder.Services.AddScoped<ICandidateSessionService, CandidateSessionService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<IAnswerService, AnswerService>();
builder.Services.AddScoped<IExamCalculationService, ExamCalculationService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();


//Exam
builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddScoped<IQuestionSetService, QuestionSetService>();
builder.Services.AddScoped<IQuestionManagementService, QuestionManagementService>();
builder.Services.AddScoped<IExamPublishingService, ExamPublishingService>();
builder.Services.AddScoped<IExamValidationService, ExamValidationService>();
builder.Services.AddScoped<IExamUtilityService, ExamUtilityService>();


//QuestionSet
builder.Services.AddScoped<IQuestionSetService, QuestionSetService>();
builder.Services.AddScoped<IViewBagPreparationService, ViewBagPreparationService>();


builder.Services.AddScoped<ICandidateSessionService, CandidateSessionService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Register background service
builder.Services.AddHostedService<QuestionGenerationBackgroundService>();

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfiles));

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
  options.IdleTimeout = TimeSpan.FromMinutes(30);
  options.Cookie.HttpOnly = true;
  options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Home/Error");
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();

}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

// Initialize layout template data
app.Use(async (context, next) =>
{
  if (!context.Session.Keys.Contains("layoutInitialized"))
  {
    context.Session.SetString("appName", "نظام اختبارات التوظيف");
    context.Session.SetString("productPage", "https://example.com");
    context.Session.SetString("layoutInitialized", "true");
  }
  await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Layout data initializer
public class LayoutDataInitializer : IStartupFilter
{
  public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
  {
    return app =>
    {
      next(app);
    };
  }
}
