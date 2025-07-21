using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using TawtheefTest.DTOs;
using AutoMapper;

namespace TawtheefTest.Services
{
  public interface IOTPService
  {
    Task<string> GenerateOTPAsync(int PhoneNumber);
    Task<bool> VerifyOTPAsync(int PhoneNumber, string otp);
    Task<string> GenerateAndSendOTP(int PhoneNumber);
  }

  public class OTPService : IOTPService
  {
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public OTPService(ApplicationDbContext context, IMapper mapper)
    {
      _context = context;
      _mapper = mapper;
    }

    public async Task<string> GenerateAndSendOTP(int PhoneNumber)
    {
      // This is a wrapper around GenerateOTPAsync that would also send the OTP to the user
      var otp = await GenerateOTPAsync(PhoneNumber);

      // In a real application, you would send the OTP via SMS or other means here
      // For now, we just return the OTP
      return otp;
    }

    public async Task<string> GenerateOTPAsync(int PhoneNumber)
    {
      // Generate a random 6-digit OTP
      var random = new Random();
      var otp = random.Next(100000, 999999).ToString();

      // Set expiration time (10 minutes from now)
      var expirationTime = DateTime.UtcNow.AddMinutes(10);

      // Create DTO first
      var otpVerificationDto = new OTPVerificationDto
      {
        PhoneNumber = PhoneNumber,
        OTPCode = otp,
        IsVerified = false,
        ExpiresAt = expirationTime,
        CreatedAt = DateTime.UtcNow
      };

      // Map DTO to data model
      var otpVerification = _mapper.Map<OtpVerification>(otpVerificationDto);

      _context.OtpVerifications.Add(otpVerification);
      await _context.SaveChangesAsync();

      // In a real application, send the OTP via email
      // For now, just return it
      return otp;
    }

    public async Task<bool> VerifyOTPAsync(int PhoneNumber, string otp)
    {
      // Find the most recent OTP for this phone number
      var otpVerificationModel = await _context.OtpVerifications
          .Where(o => o.PhoneNumber == PhoneNumber && !o.IsVerified)
          .OrderByDescending(o => o.CreatedAt)
          .FirstOrDefaultAsync();

      if (otpVerificationModel == null)
      {
        return false;
      }

      // Map to DTO
      var otpVerificationDto = _mapper.Map<OTPVerificationDto>(otpVerificationModel);

      // Check if OTP is expired
      if (DateTime.UtcNow > otpVerificationDto.ExpiresAt)
      {
        return false;
      }

      // Check if OTP matches
      if (otpVerificationDto.OTPCode != otp)
      {
        return false;
      }

      // Mark OTP as verified
      otpVerificationModel.IsVerified = true;
      await _context.SaveChangesAsync();

      return true;
    }
  }
}
