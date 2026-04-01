using System.Collections.Generic;

namespace прпгр
{
    public class MaterialViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Subject { get; set; }
        public string? Course { get; set; }
        public string? Topic { get; set; }
        public List<string>? Tags { get; set; }
        public string? ShortDescription { get; set; }
        public bool IsPremium { get; set; }
        public string Status { get; set; } = null!;
        public double AverageRating { get; set; }
        public int RatingsCount { get; set; }
    }
}
