using System;

namespace прпгр.Models
{
    public class UserActivity
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public int MaterialId { get; set; }
        public string ActivityType { get; set; } = ""; // "View", "Download", "Rate"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
