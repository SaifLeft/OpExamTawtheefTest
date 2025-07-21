using System;

namespace TawtheefTest.DTOs
{
  public class OTPDto
  {
    public int PhoneNumber { get; set; }
    public string OTPCode { get; set; }
    public bool IsVerified { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
  }

  public class OTPRequestDto
  {
    public int PhoneNumber { get; set; }
  }

  public class OTPVerificationDto
  {
    public int PhoneNumber { get; set; }
    public string OTPCode { get; set; }
    public bool IsVerified { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
  }
}
