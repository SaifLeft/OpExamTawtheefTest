using System;

namespace TawtheefTest.ViewModels
{
    public class NotificationViewModel
    {
        public long Id { get; set; }
        public long CandidateId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } // info, success, warning, danger
        public long IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public string TimeSince { get; set; }
    }
}
