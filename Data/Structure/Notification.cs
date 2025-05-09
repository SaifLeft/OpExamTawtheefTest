using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TawtheefTest.Data.Structure
{
  public class Notification
  {
    [Key]
    public int Id { get; set; }

    [Required]
    public int CandidateId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; }

    [Required]
    public string Message { get; set; }

    [MaxLength(50)]
    public string Type { get; set; } = "info"; // info, success, warning, danger

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }

    [ForeignKey("CandidateId")]
    public virtual Candidate Candidate { get; set; }
  }
}
