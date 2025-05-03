using System.Threading.Tasks;

namespace TawtheefTest.Services
{
    public interface IOTPService
    {
        Task<string> GenerateAndSendOTP(string phoneNumber);
        Task<bool> VerifyOTP(string phoneNumber, string otpCode);
    }
}