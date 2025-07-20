using System;

namespace TawtheefTest.DTOs
{
    public class OTPDto
    {
        public long PhoneNumber { get; set; }
        public string OTPCode { get; set; }
        public long IsVerified { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class OTPRequestDto
    {
        public long PhoneNumber { get; set; }
    }

    public class OTPVerificationDto
    {
        public long PhoneNumber { get; set; }
        public string OTPCode { get; set; }
        public long IsVerified { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
