using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace прпгр.Models
{
    public class LMSAccountLink
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public string LmsUrl { get; set; } = "";
        public string LmsToken { get; set; } = "";
        public string LmsType { get; set; } = "Moodle";

        // EF stores as JSON string; property exposes as List<string>
        public string LinkedCourseIdsJson { get; set; } = "[]";

        [NotMapped]
        public List<string> LinkedCourseIds
        {
            get => JsonSerializer.Deserialize<List<string>>(LinkedCourseIdsJson ?? "[]") ?? new();
            set => LinkedCourseIdsJson = JsonSerializer.Serialize(value ?? new List<string>());
        }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
