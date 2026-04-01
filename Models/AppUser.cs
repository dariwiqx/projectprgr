using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace прпгр.Models
{
    public class AppUser
    {
        [Key]
        public string Id { get; set; } = "";

        [Required]
        [MaxLength(100)]
        public string UserName { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        public ICollection<RewardTransaction> RewardTransactions { get; set; } = new List<RewardTransaction>();

        public string Role { get; set; } = "Student";
        public bool IsBlocked { get; set; }
        public int Balance { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
