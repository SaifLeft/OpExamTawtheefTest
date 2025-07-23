using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TawtheefTest.Data.Structure;

namespace TawtheefTest.Services
{
  public interface INotificationService
  {
    Task<List<Notification>> GetCandidateNotificationsAsync(int candidateId);
    Task CreateNotificationAsync(int candidateId, string title, string message, string type = "info");
    Task MarkAsReadAsync(int notificationId);
    Task<int> GetUnreadCountAsync(int candidateId);
  }

  public class NotificationService : INotificationService
  {
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
      _context = context;
    }

    public async Task<List<Notification>> GetCandidateNotificationsAsync(int candidateId)
    {
      return await _context.Notifications
          .Where(n => n.CandidateId == candidateId)
          .OrderByDescending(n => n.CreatedAt)
          .ToListAsync();
    }

    public async Task CreateNotificationAsync(int candidateId, string title, string message, string type = "info")
    {
      var notification = new Notification
      {
        CandidateId = candidateId,
        Title = title,
        Message = message,
        Type = type,
        IsRead = false,
        CreatedAt = DateTime.Now
      };

      _context.Notifications.Add(notification);
      await _context.SaveChangesAsync();
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
      var notification = await _context.Notifications.FindAsync(notificationId);
      if (notification != null)
      {
        notification.IsRead = true;
        notification.ReadAt = DateTime.Now;
        await _context.SaveChangesAsync();
      }
    }

    public async Task<int> GetUnreadCountAsync(int candidateId)
    {
      return await _context.Notifications
          .Where(n => n.CandidateId == candidateId && !n.IsRead)
          .CountAsync();
    }
  }
}
