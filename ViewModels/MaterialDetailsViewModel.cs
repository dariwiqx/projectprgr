using System.Collections.Generic;

namespace прпгр.Models
{
    public class MaterialDetailsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public string Subject { get; set; } = "";
        public string Course { get; set; } = "";
        public string? Topic { get; set; }
        public string FilePath { get; set; } = "";
        public List<string> Tags { get; set; } = new();
        public double AverageRating { get; set; }
        public int RatingsCount { get; set; }
        public IEnumerable<MaterialRating> Ratings { get; set; } = new List<MaterialRating>();
        public string Status { get; set; } = "";
        public bool IsPremium { get; set; }
        public string AuthorId { get; set; } = "";
        public int ComplaintsCount { get; set; }
    }
}
