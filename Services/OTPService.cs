using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;

namespace TawtheefTest.Services
{
  public interface IOTPService
  {
    Task<string> GenerateOTPAsync(int PhoneNumber);
    Task<bool> VerifyOTPAsync(int PhoneNumber, string otp);
  }

  public class OTPService : IOTPService
  {
    private readonly ApplicationDbContext _context;

    public OTPService(ApplicationDbContext context)
    {
      _context = context;
    }

    public async Task<string> GenerateOTPAsync(int PhoneNumber)
    {
      // Generate a random 6-digit OTP
      var random = new Random();
      var otp = random.Next(100000, 999999).ToString();

      // Set expiration time (10 minutes from now)
      var expirationTime = DateTime.UtcNow.AddMinutes(10);

      // Save OTP to database
      var otpVerification = new OTPVerification
      {
        PhoneNumber = PhoneNumber,
        OTPCode = otp,
        IsVerified = false,
        ExpiresAt = expirationTime,
        CreatedAt = DateTime.UtcNow
      };

      _context.OTPVerifications.Add(otpVerification);
      await _context.SaveChangesAsync();

      // In a real application, send the OTP via email
      // For now, just return it
      return otp;
    }

    public async Task<bool> VerifyOTPAsync(int PhoneNumber, string otp)
    {
      // Find the most recent OTP for this email
      var otpVerification = await _context.OTPVerifications
          .Where(o => o.PhoneNumber == PhoneNumber && !o.IsVerified)
          .OrderByDescending(o => o.CreatedAt)
          .FirstOrDefaultAsync();

      if (otpVerification == null)
      {
        return false;
      }

      // Check if OTP is expired
      if (DateTime.UtcNow > otpVerification.ExpiresAt)
      {
        return false;
      }

      // Check if OTP matches
      if (otpVerification.OTPCode != otp)
      {
        return false;
      }

      // Mark OTP as verified
      otpVerification.IsVerified = true;
      await _context.SaveChangesAsync();

      return true;
    }
  }
}
