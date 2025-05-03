using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using System.Threading.Tasks;
using TawtheefTest.Services;

namespace TawtheefTest.Controllers
{
  public class AuthController : Controller
  {
    private readonly ApplicationDbContext _context;
    private readonly IOTPService _otpService;

    public AuthController(ApplicationDbContext context, IOTPService otpService)
    {
      _context = context;
      _otpService = otpService;
    }

    // GET: Auth/Login
    public IActionResult Login()
    {
      return View();
    }

    // POST: Auth/SendOTP
    [HttpPost]
    public async Task<IActionResult> SendOTP(string phoneNumber)
    {
      if (string.IsNullOrEmpty(phoneNumber))
      {
        TempData["ErrorMessage"] = "Phone number is required.";
        return RedirectToAction(nameof(Login));
      }

      // Check if the candidate with the phone number exists
      var candidate = await _context.Candidates
          .FirstOrDefaultAsync(c => c.Phone.ToString() == phoneNumber);

      if (candidate == null)
      {
        TempData["ErrorMessage"] = "No candidate found with this phone number.";
        return RedirectToAction(nameof(Login));
      }

      // Generate and send OTP
      var otpCode = await _otpService.GenerateAndSendOTP(int.Parse(phoneNumber));

      TempData["SuccessMessage"] = "OTP sent to your phone number.";
      TempData["PhoneNumber"] = phoneNumber;

      // For demo purposes only, we're storing the OTP in TempData
      // In a real application, this would be sent via SMS and not stored in session
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
      TempData.Keep("OTPCode"); // For demo purposes only

      return View();
    }

    // POST: Auth/VerifyOTP
    [HttpPost]
    public async Task<IActionResult> VerifyOTP(string phoneNumber, string otpCode)
    {
      if (string.IsNullOrEmpty(otpCode))
      {
        TempData["ErrorMessage"] = "OTP code is required.";
        return RedirectToAction(nameof(VerifyOTP));
      }

      // Check if the OTP code matches the one sent
      var sentOtpCode = TempData["OTPCode"]?.ToString();
      if (string.IsNullOrEmpty(sentOtpCode) || sentOtpCode != otpCode)
      {
        TempData["ErrorMessage"] = "Invalid OTP code.";
        return RedirectToAction(nameof(VerifyOTP));
      }

      // Verify OTP
      bool isValid = await _otpService.VerifyOTPAsync(int.Parse(phoneNumber), otpCode);

      if (!isValid)
      {
        TempData["ErrorMessage"] = "Invalid OTP or OTP has expired. Please try again.";
        TempData["PhoneNumber"] = phoneNumber;
        return RedirectToAction(nameof(VerifyOTP));
      }

      // Get candidate
      var candidate = await _context.Candidates
          .FirstOrDefaultAsync(c => c.Phone.ToString() == phoneNumber);

      if (candidate == null)
      {
        TempData["ErrorMessage"] = "No candidate found with this phone number.";
        return RedirectToAction(nameof(Login));
      }

      // Store candidate information in session
      HttpContext.Session.SetInt32("CandidateId", candidate.Id);
      HttpContext.Session.SetString("CandidateName", candidate.Name);

      return RedirectToAction("Index", "CandidateExams");
    }

    // GET: Auth/Logout
    public IActionResult Logout()
    {
      // Clear session
      HttpContext.Session.Clear();

      return RedirectToAction(nameof(Login));
    }
  }
}
