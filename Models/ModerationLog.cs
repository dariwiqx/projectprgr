using System;

namespace прпгр.Models
{
    public class ModerationLog
    {
        public int Id { get; set; }
        public int MaterialId { get; set; }
        public string ModeratorId { get; set; } = "";
        public string OldStatus { get; set; } = "";
        public string NewStatus { get; set; } = "";
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
