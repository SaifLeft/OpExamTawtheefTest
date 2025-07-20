using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using System.Threading.Tasks;
using TawtheefTest.Services;
using AutoMapper;
using System;
using System.Linq;
using TawtheefTest.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TawtheefTest.Controllers
{
  public class AuthController : Controller
  {
    private readonly ApplicationDbContext _context;
    private readonly IOTPService _otpService;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;

    public AuthController(ApplicationDbContext context, IOTPService otpService, IMapper mapper, INotificationService notificationService)
    {
      _context = context;
      _otpService = otpService;
      _mapper = mapper;
      _notificationService = notificationService;
    }

    // GET: Auth/Login
    public IActionResult Login()
    {
      // التحقق إذا كان المستخدم مسجل دخول مسبقاً
      if (HttpContext.Session.GetInt32("CandidateId") != null)
      {
        return RedirectToAction("Index", "CandidateExams");
      }
      return View();
    }

    // POST: Auth/SendOTP
    [HttpPost]
    public async Task<IActionResult> SendOTP(string phoneNumber)
    {
      if (string.IsNullOrEmpty(phoneNumber))
      {
        TempData["ErrorMessage"] = "رقم الهاتف مطلوب.";
        return RedirectToAction(nameof(Login));
      }

      // التحقق من صحة تنسيق رقم الهاتف
      if (!int.TryParse(phoneNumber, out int phoneNumberInt))
      {
        TempData["ErrorMessage"] = "صيغة رقم الهاتف غير صحيحة.";
        return RedirectToAction(nameof(Login));
      }

      // البحث عن المرشح برقم الهاتف
      var candidate = await _context.Candidates
          .Include(c => c.CandidateExams)
          .FirstOrDefaultAsync(c => c.Phone.ToString() == phoneNumber);

      if (candidate == null)
      {
        TempData["ErrorMessage"] = "لم يتم العثور على مرشح بهذا الرقم.";
        return RedirectToAction(nameof(Login));
      }

      // التحقق من حالة المرشح
      if (candidate.IsActive == 0)
      {
        TempData["ErrorMessage"] = "هذا الحساب غير نشط. يرجى الاتصال بالإدارة.";
        return RedirectToAction(nameof(Login));
      }

      // إنشاء وإرسال رمز OTP
      var otpCode = await _otpService.GenerateAndSendOTP(phoneNumberInt);

      TempData["SuccessMessage"] = "تم إرسال رمز التحقق إلى رقم هاتفك.";
      TempData["PhoneNumber"] = phoneNumber;

      // للعرض التجريبي فقط، نقوم بتخزين الرمز في TempData
      // في التطبيق الحقيقي، سيتم إرساله عبر الرسائل القصيرة SMS وليس تخزينه في الجلسة
      TempData["OTPCode"] = otpCode;

      return RedirectToAction(nameof(VerifyOTP));
    }

    // GET: Auth/VerifyOTP
    public IActionResult VerifyOTP()
    {
      var phoneNumber = TempData["PhoneNumber"]?.ToString();
      if (string.IsNullOrEmpty(phoneNumber))
      {
        return RedirectToAction(nameof(Login));
      }

      ViewData["PhoneNumber"] = phoneNumber;
      TempData.Keep("PhoneNumber");
      TempData.Keep("OTPCode"); // للعرض التجريبي فقط

      return View();
    }

    // POST: Auth/VerifyOTP
    [HttpPost]
    public async Task<IActionResult> VerifyOTP(string phoneNumber, string otpCode)
    {
      if (string.IsNullOrEmpty(otpCode))
      {
        TempData["ErrorMessage"] = "رمز التحقق مطلوب.";
        TempData["PhoneNumber"] = phoneNumber;
        return RedirectToAction(nameof(VerifyOTP));
      }

      // التحقق من تطابق رمز التحقق مع الرمز المرسل
      var sentOtpCode = TempData["OTPCode"]?.ToString();
      if (string.IsNullOrEmpty(sentOtpCode) || sentOtpCode != otpCode)
      {
        TempData["ErrorMessage"] = "رمز التحقق غير صحيح.";
        TempData["PhoneNumber"] = phoneNumber;
        return RedirectToAction(nameof(VerifyOTP));
      }

      // التحقق من صلاحية رمز التحقق
      bool isValid = await _otpService.VerifyOTPAsync(int.Parse(phoneNumber), otpCode);

      if (!isValid)
      {
        TempData["ErrorMessage"] = "رمز التحقق غير صالح أو منتهي الصلاحية. يرجى المحاولة مرة أخرى.";
        TempData["PhoneNumber"] = phoneNumber;
        return RedirectToAction(nameof(VerifyOTP));
      }

      // الحصول على بيانات المرشح
      var candidate = await _context.Candidates
          .Include(c => c.CandidateExams)
          .FirstOrDefaultAsync(c => c.Phone.ToString() == phoneNumber);

      if (candidate == null)
      {
        TempData["ErrorMessage"] = "لم يتم العثور على مرشح بهذا الرقم.";
        return RedirectToAction(nameof(Login));
      }

      // تسجيل آخر تسجيل دخول
      candidate.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
      await _context.SaveChangesAsync();

      // تخزين معلومات المرشح في الجلسة
      HttpContext.Session.SetInt32("CandidateId", candidate.Id);
      HttpContext.Session.SetString("CandidateName", candidate.Name);

      // إنشاء إشعار ترحيبي للمرشح
      await _notificationService.CreateNotificationAsync(
          candidate.Id,
          "مرحباً بعودتك!",
          $"مرحباً {candidate.Name}، تم تسجيل دخولك بنجاح. لديك {await _context.Assignments.CountAsync(ce => ce.CandidateId == candidate.Id && ce.Status == "InProgress")} اختبارات قيد التنفيذ.",
          "success"
      );

      TempData["SuccessMessage"] = $"مرحباً {candidate.Name}، تم تسجيل دخولك بنجاح.";
      return RedirectToAction("Index", "CandidateExams");
    }

    // GET: Auth/Logout
    public IActionResult Logout()
    {
      // مسح الجلسة
      HttpContext.Session.Clear();

      TempData["SuccessMessage"] = "تم تسجيل الخروج بنجاح.";
      return RedirectToAction(nameof(Login));
    }

    // GET: Auth/Register
    public IActionResult Register()
    {
      ViewBag.Jobs = new SelectList(_context.Jobs, "Id", "Title");
      return View();
    }

    // POST: Auth/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(CreateCandidatesVM model)
    {
      if (ModelState.IsValid)
      {
        // التحقق من عدم وجود رقم هاتف مسجل مسبقاً
        if (await _context.Candidates.AnyAsync(c => c.Phone == model.Phone))
        {
          ModelState.AddModelError("Phone", "هذا الرقم مسجل بالفعل.");
          ViewBag.Jobs = new SelectList(_context.Jobs, "Id", "Title", model.JobId);
          return View(model);
        }

        var candidate = new Candidate
        {
          Name = model.Name,
          Phone = model.Phone,
          JobId = model.JobId,
          IsActive = true,
          CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };

        _context.Add(candidate);
        await _context.SaveChangesAsync();

        // إنشاء إشعار ترحيبي للمرشح
        await _notificationService.CreateNotificationAsync(
            candidate.Id,
            "مرحباً بك في منصة الاختبارات",
            $"مرحباً {candidate.Name}، نرحب بك في منصة اختبارات المرشحين. يمكنك الآن تسجيل الدخول وبدء الاختبارات المتاحة لك.",
            "info"
        );

        TempData["SuccessMessage"] = "تم تسجيلك بنجاح. يمكنك الآن تسجيل الدخول.";
        return RedirectToAction(nameof(Login));
      }

      ViewBag.Jobs = new SelectList(_context.Jobs, "Id", "Title", model.JobId);
      return View(model);
    }
  }
}
