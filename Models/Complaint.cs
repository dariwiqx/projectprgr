using System;

namespace прпгр.Models
{
    public class Complaint
    {
        public int Id { get; set; }
        public int MaterialId { get; set; }
        public string UserId { get; set; } = "";
        public string Reason { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsResolved { get; set; }
    }
}
