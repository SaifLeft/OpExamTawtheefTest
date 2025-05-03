using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AutoMapper;
using TawtheefTest.Services;
using TawtheefTest.ViewModels;
using TawtheefTest.DTOs;

namespace TawtheefTest.Controllers
{
  public class OTPController : Controller
  {
    private readonly IOTPService _otpService;
    private readonly IMapper _mapper;

    public OTPController(IOTPService otpService, IMapper mapper)
    {
      _otpService = otpService;
      _mapper = mapper;
    }

    // GET: OTP/Request
    public IActionResult Request()
    {
      return View(new OTPRequestViewModel());
    }

    // POST: OTP/Request
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Request(OTPRequestViewModel viewModel)
    {
      if (ModelState.IsValid)
      {
        // Map ViewModel to DTO
        var requestDto = _mapper.Map<OTPRequestDto>(viewModel);

        // Call service method with DTO
        var otp = await _otpService.GenerateAndSendOTP(requestDto.PhoneNumber);

        // Store phone number in temp data for the verification page
        TempData["PhoneNumber"] = viewModel.PhoneNumber;

        return RedirectToAction(nameof(Verify));
      }

      return View(viewModel);
    }

    // GET: OTP/Verify
    public IActionResult Verify()
    {
      // Get phone number from temp data
      if (TempData.TryGetValue("PhoneNumber", out var phoneNumber))
      {
        var viewModel = new OTPVerificationViewModel
        {
          PhoneNumber = (int)phoneNumber
        };

        // Keep the phone number in temp data for post action
        TempData["PhoneNumber"] = phoneNumber;

        return View(viewModel);
      }

      return RedirectToAction(nameof(Request));
    }

    // POST: OTP/Verify
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(OTPVerificationViewModel viewModel)
    {
      if (ModelState.IsValid)
      {
        // Map ViewModel to DTO
        var verificationDto = _mapper.Map<OTPVerificationDto>(viewModel);

        // Call service method with DTO
        var isVerified = await _otpService.VerifyOTPAsync(verificationDto.PhoneNumber, verificationDto.OTPCode);

        if (isVerified)
        {
          TempData["SuccessMessage"] = "تم التحقق بنجاح";
          return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError("", "رمز التحقق غير صحيح أو منتهي الصلاحية");
      }

      return View(viewModel);
    }
  }
}
