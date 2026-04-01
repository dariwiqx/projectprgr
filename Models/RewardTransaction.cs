using System;
using System.ComponentModel.DataAnnotations;

namespace прпгр.Models
{
    public class RewardTransaction
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = null!;

        public int Points { get; set; }

        public int? MaterialId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
