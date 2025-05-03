using System;

namespace TawtheefTest.Models
{
    public class OTPVerification
    {
        public int Id { get; set; }
        public string PhoneNumber { get; set; }
        public string OTPCode { get; set; }
        public DateTime ExpiryTime { get; set; }
        public bool IsUsed { get; set; }
    }
}