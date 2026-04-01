using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace прпгр.Models
{
    public class CreateMaterialViewModel
    {
        [Required]
        [MaxLength(200)]
        [Display(Name = "Название")]
        public string Title { get; set; } = null!;

        [MaxLength(2000)]
        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Предмет")]
        public string Subject { get; set; } = null!;

        [Required]
        [Display(Name = "Курс")]
        public string Course { get; set; } = null!;

        [Display(Name = "Тема")]
        public string? Topic { get; set; }

        [Required]
        [Display(Name = "Файл")]
        public IFormFile File { get; set; } = null!;

        public bool IsPremium { get; set; }
        public string? TagsInput { get; set; } // "алгебра, 1 курс, контрольная"


    }
}
