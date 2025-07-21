using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;

namespace TawtheefTest.Services
{
  // ICandidateSessionService.cs
  public interface ICandidateSessionService
  {
    bool IsCandidateLoggedIn();
    int? GetCurrentCandidateId();
    Task<Candidate> GetCurrentCandidateAsync();
    void ClearSession();

    // New authentication methods
    void SetCandidateSession(Candidate candidate);
    Task<Candidate> GetCandidateByPhoneAsync(string phoneNumber);
    Task<bool> ValidateCandidateStatusAsync(Candidate candidate);
    Task UpdateLastLoginAsync(int candidateId);
    Task<int> GetInProgressExamsCountAsync(int candidateId);


  }

  // CandidateSessionService.cs
  public class CandidateSessionService : ICandidateSessionService
  {
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationDbContext _context;

    public CandidateSessionService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext context)
    {
      _httpContextAccessor = httpContextAccessor;
      _context = context;
    }

    public bool IsCandidateLoggedIn()
    {
      return _httpContextAccessor.HttpContext?.Session.GetInt32("CandidateId") != null;
    }

    public int? GetCurrentCandidateId()
    {
      return _httpContextAccessor.HttpContext?.Session.GetInt32("CandidateId");
    }

    public async Task<Candidate> GetCurrentCandidateAsync()
    {
      var candidateId = GetCurrentCandidateId();
      if (candidateId == null) return null;

      return await _context.Candidates
          .Include(c => c.Job)
          .FirstOrDefaultAsync(c => c.Id == candidateId.Value);
    }

    public void ClearSession()
    {
      _httpContextAccessor.HttpContext?.Session.Clear();
    }

    // New authentication methods
    public void SetCandidateSession(Candidate candidate)
    {
      var session = _httpContextAccessor.HttpContext?.Session;
      if (session != null)
      {
        session.SetInt32("CandidateId", candidate.Id);
        session.SetString("CandidateName", candidate.Name);
      }
    }

    public async Task<Candidate> GetCandidateByPhoneAsync(string phoneNumber)
    {
      if (string.IsNullOrEmpty(phoneNumber) || !int.TryParse(phoneNumber, out _))
        return null;

      return await _context.Candidates
          .Include(c => c.Assignments)
          .FirstOrDefaultAsync(c => c.Phone.ToString() == phoneNumber);
    }

    public async Task<bool> ValidateCandidateStatusAsync(Candidate candidate)
    {
      return candidate != null && candidate.IsActive;
    }

    public async Task UpdateLastLoginAsync(int candidateId)
    {
      var candidate = await _context.Candidates.FindAsync(candidateId);
      if (candidate != null)
      {
        candidate.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
      }
    }

    public async Task<int> GetInProgressExamsCountAsync(int candidateId)
    {
      return await _context.Assignments
          .CountAsync(ce => ce.CandidateId == candidateId && ce.Status == "InProgress");
    }
  }
}
