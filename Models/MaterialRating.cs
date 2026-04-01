using System;

namespace прпгр.Models
{
    public class MaterialRating
    {
        public int Id { get; set; }
        public int MaterialId { get; set; }
        public string UserId { get; set; } = "";
        public int Score { get; set; } // 1–5
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
