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
    private readonly ICandidateSessionService _sessionService;
    private readonly IAuthenticationService _authService;
    private readonly IViewBagPreparationService _viewDataService;
    private readonly IMapper _mapper;

    public AuthController(
        ApplicationDbContext context,
        ICandidateSessionService sessionService,
        IAuthenticationService authService,
        IViewBagPreparationService viewDataService,
        IMapper mapper)
    {
      _context = context;
      _sessionService = sessionService;
      _authService = authService;
      _viewDataService = viewDataService;
      _mapper = mapper;
    }

    // GET: Auth/Login
    public IActionResult Login()
    {
      // Check if user is already logged in
      if (_sessionService.IsCandidateLoggedIn())
      {
        return RedirectToAction("Index", "CandidateExams");
      }
      return View();
    }

    // POST: Auth/SendOTP
    [HttpPost]
    public async Task<IActionResult> SendOTP(string phoneNumber)
    {
      var result = await _authService.SendOtpAsync(phoneNumber);

      if (!result.Success)
      {
        TempData["ErrorMessage"] = result.ErrorMessage;
        return RedirectToAction(nameof(Login));
      }

      TempData["SuccessMessage"] = "تم إرسال رمز التحقق إلى رقم هاتفك.";
      TempData["PhoneNumber"] = phoneNumber;

      // For demo purposes only
      TempData["OTPCode"] = result.OtpCode;

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

      _viewDataService.SetPhoneNumberInViewData(this, phoneNumber);
      _viewDataService.KeepTempData(this, "PhoneNumber", "OTPCode");

      return View();
    }

    // POST: Auth/VerifyOTP
    [HttpPost]
    public async Task<IActionResult> VerifyOTP(string phoneNumber, string otpCode)
    {
      var verificationResult = await _authService.VerifyOtpAsync(phoneNumber, otpCode);

      if (!verificationResult.Success)
      {
        TempData["ErrorMessage"] = verificationResult.ErrorMessage;
        TempData["PhoneNumber"] = phoneNumber;
        return RedirectToAction(nameof(VerifyOTP));
      }

      var loginResult = await _authService.CompleteLoginAsync(verificationResult.Candidate);

      if (!loginResult.Success)
      {
        TempData["ErrorMessage"] = loginResult.Message;
        return RedirectToAction(nameof(Login));
      }

      TempData["SuccessMessage"] = loginResult.Message;
      return RedirectToAction("Index", "CandidateExams");
    }

    // GET: Auth/Logout
    public IActionResult Logout()
    {
      _sessionService.ClearSession();
      TempData["SuccessMessage"] = "تم تسجيل الخروج بنجاح.";
      return RedirectToAction(nameof(Login));
    }

    // GET: Auth/Register
    public IActionResult Register()
    {
      _viewDataService.SetJobsSelectList(this);
      return View();
    }

    // POST: Auth/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(CreateCandidatesVM model)
    {
      if (ModelState.IsValid)
      {
        var validation = await _authService.ValidateRegistrationAsync(model);
        if (!validation.Success)
        {
          ModelState.AddModelError("Phone", validation.ErrorMessage);
          _viewDataService.SetJobsSelectList(this, model.JobId);
          return View(model);
        }

        var candidate = await _authService.RegisterCandidateAsync(model);

        TempData["SuccessMessage"] = "تم تسجيلك بنجاح. يمكنك الآن تسجيل الدخول.";
        return RedirectToAction(nameof(Login));
      }

      _viewDataService.SetJobsSelectList(this, model.JobId);
      return View(model);
    }
  }
}
