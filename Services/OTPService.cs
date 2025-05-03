using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using System;
using System.Linq;
using System.Threading.Tasks;
using TawtheefTest.Models;

namespace TawtheefTest.Services
{
    public class OTPService : IOTPService
    {
        private readonly ApplicationDbContext _context;
        private readonly Random _random;

        public OTPService(ApplicationDbContext context)
        {
            _context = context;
            _random = new Random();
        }

        public async Task<string> GenerateAndSendOTP(string phoneNumber)
        {
            // Generate a 6-digit OTP code
            string otpCode = _random.Next(100000, 999999).ToString();

            // Store the code in the database
            var verification = new OTPVerification
            {
                PhoneNumber = phoneNumber,
                OTPCode = otpCode,
                ExpiryTime = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false
            };

            _context.OTPVerifications.Add(verification);
            await _context.SaveChangesAsync();

            // In this demo version, we assume the code was sent to the phone
            // In a real application, an SMS service would be used to send the code

            return otpCode; // We return the code for testing only
        }

        public async Task<bool> VerifyOTP(string phoneNumber, string otpCode)
        {
            var verification = await _context.OTPVerifications
                .Where(v => v.PhoneNumber == phoneNumber && v.OTPCode == otpCode && !v.IsUsed)
                .OrderByDescending(v => v.ExpiryTime)
                .FirstOrDefaultAsync();

            if (verification == null || verification.ExpiryTime < DateTime.UtcNow)
            {
                return false;
            }

            verification.IsUsed = true;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
