using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using TawtheefTest.Data.Structure;
using TawtheefTest.ViewModels;

namespace TawtheefTest.Services
{
  // IAuthenticationService.cs
  public interface IAuthenticationService
  {
    Task<(bool Success, string ErrorMessage)> ValidatePhoneNumberAsync(string phoneNumber);
    Task<(bool Success, string ErrorMessage, string OtpCode)> SendOtpAsync(string phoneNumber);
    Task<(bool Success, string ErrorMessage, Candidate Candidate)> VerifyOtpAsync(string phoneNumber, string otpCode);
    Task<(bool Success, string ErrorMessage)> ValidateRegistrationAsync(CreateCandidatesVM model);
    Task<Candidate> RegisterCandidateAsync(CreateCandidatesVM model);
    Task<(bool Success, string Message)> CompleteLoginAsync(Candidate candidate);
    Task<string> CreateWelcomeMessageAsync(Candidate candidate);
  }

  // AuthenticationService.cs
  public class AuthenticationService : IAuthenticationService
  {
    private readonly ApplicationDbContext _context;
    private readonly ICandidateSessionService _sessionService;
    private readonly IOTPService _otpService;
    private readonly INotificationService _notificationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITempDataDictionaryFactory _tempDataFactory;

    public AuthenticationService(
        ApplicationDbContext context,
        ICandidateSessionService sessionService,
        IOTPService otpService,
        INotificationService notificationService,
        IHttpContextAccessor httpContextAccessor,
        ITempDataDictionaryFactory tempDataFactory)
    {
      _context = context;
      _sessionService = sessionService;
      _otpService = otpService;
      _notificationService = notificationService;
      _httpContextAccessor = httpContextAccessor;
      _tempDataFactory = tempDataFactory;
    }

    public async Task<(bool Success, string Message)> CompleteLoginAsync(Candidate candidate)
    {
      try
      {
        // Update last login
        await _sessionService.UpdateLastLoginAsync(candidate.Id);

        // Set session
        _sessionService.SetCandidateSession(candidate);

        // Create welcome notification
        var inProgressCount = await _sessionService.GetInProgressExamsCountAsync(candidate.Id);
        await _notificationService.CreateNotificationAsync(
            candidate.Id,
            "مرحباً بعودتك!",
            $"مرحباً {candidate.Name}، تم تسجيل دخولك بنجاح. لديك {inProgressCount} اختبارات قيد التنفيذ.",
            "success"
        );

        var message = await CreateWelcomeMessageAsync(candidate);
        return (true, message);
      }
      catch (Exception ex)
      {
        return (false, $"حدث خطأ أثناء تسجيل الدخول: {ex.Message}");
      }
    }

    public async Task<string> CreateWelcomeMessageAsync(Candidate candidate)
    {
      return $"مرحباً {candidate.Name}، تم تسجيل دخولك بنجاح.";
    }

    public async Task<(bool Success, string ErrorMessage)> ValidatePhoneNumberAsync(string phoneNumber)
    {
      if (string.IsNullOrEmpty(phoneNumber))
        return (false, "رقم الهاتف مطلوب.");

      if (!int.TryParse(phoneNumber, out _))
        return (false, "صيغة رقم الهاتف غير صحيحة.");

      var candidate = await _sessionService.GetCandidateByPhoneAsync(phoneNumber);
      if (candidate == null)
        return (false, "لم يتم العثور على مرشح بهذا الرقم.");

      if (!await _sessionService.ValidateCandidateStatusAsync(candidate))
        return (false, "هذا الحساب غير نشط. يرجى الاتصال بالإدارة.");

      return (true, string.Empty);
    }

    public async Task<(bool Success, string ErrorMessage, string OtpCode)> SendOtpAsync(string phoneNumber)
    {
      var validation = await ValidatePhoneNumberAsync(phoneNumber);
      if (!validation.Success)
        return (false, validation.ErrorMessage, string.Empty);

      try
      {
        var otpCode = await _otpService.GenerateAndSendOTP(int.Parse(phoneNumber));

        // For demo purposes only - in production, OTP should not be stored in TempData
        if (_httpContextAccessor.HttpContext != null)
        {
          var tempData = _tempDataFactory.GetTempData(_httpContextAccessor.HttpContext);
          tempData["OTPCode"] = otpCode;
        }

        return (true, string.Empty, otpCode);
      }
      catch (Exception ex)
      {
        return (false, $"خطأ في إرسال رمز التحقق: {ex.Message}", string.Empty);
      }
    }

    public async Task<(bool Success, string ErrorMessage, Candidate Candidate)> VerifyOtpAsync(string phoneNumber, string otpCode)
    {
      if (string.IsNullOrEmpty(otpCode))
        return (false, "رمز التحقق مطلوب.", null);

      // Demo verification - in production, use proper OTP verification
      string sentOtpCode = null;
      if (_httpContextAccessor.HttpContext != null)
      {
        var tempData = _tempDataFactory.GetTempData(_httpContextAccessor.HttpContext);
        sentOtpCode = tempData["OTPCode"]?.ToString();
      }

      if (string.IsNullOrEmpty(sentOtpCode) || sentOtpCode != otpCode)
        return (false, "رمز التحقق غير صحيح.", null);

      // Verify OTP with service
      bool isValid = await _otpService.VerifyOTPAsync(int.Parse(phoneNumber), otpCode);
      if (!isValid)
        return (false, "رمز التحقق غير صالح أو منتهي الصلاحية. يرجى المحاولة مرة أخرى.", null);

      // Get candidate
      var candidate = await _sessionService.GetCandidateByPhoneAsync(phoneNumber);
      if (candidate == null)
        return (false, "لم يتم العثور على مرشح بهذا الرقم.", null);

      return (true, string.Empty, candidate);
    }

    public async Task<(bool Success, string ErrorMessage)> ValidateRegistrationAsync(CreateCandidatesVM model)
    {
      if (await _context.Candidates.AnyAsync(c => c.Phone == model.Phone))
        return (false, "هذا الرقم مسجل بالفعل.");

      return (true, string.Empty);
    }

    public async Task<Candidate> RegisterCandidateAsync(CreateCandidatesVM model)
    {
      var candidate = new Candidate
      {
        Name = model.Name,
        Phone = model.Phone,
        JobId = model.JobId,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
      };

      _context.Add(candidate);
      await _context.SaveChangesAsync();

      // Create welcome notification
      await _notificationService.CreateNotificationAsync(
          candidate.Id,
          "مرحباً بك في منصة الاختبارات",
          $"مرحباً {candidate.Name}، نرحب بك في منصة اختبارات المرشحين. يمكنك الآن تسجيل الدخول وبدء الاختبارات المتاحة لك.",
          "info"
      );

      return candidate;
    }
  }
}
