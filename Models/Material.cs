using System;
using System.ComponentModel.DataAnnotations;

namespace прпгр.Models
{
    public class Material
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = null!;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(100)]
        public string Subject { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string Course { get; set; } = null!;

        [MaxLength(200)]
        public string? Topic { get; set; }

        [Required]
        public string FilePath { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        [Required]
        public string AuthorId { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsPremium { get; set; }

        public double AverageRating { get; set; }
        public int RatingsCount { get; set; }
        public int DownloadCount { get; set; }
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    }
}
