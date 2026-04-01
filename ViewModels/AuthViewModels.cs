using System.ComponentModel.DataAnnotations;

namespace прпгр.Models
{
    public class RegisterViewModel
    {
        [Required]
        [MaxLength(100)]
        [Display(Name = "Логин")]
        public string UserName { get; set; } = "";

        [Required]
        [MaxLength(100)]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } ="";
    }

    public class LoginViewModel
    {
        [Required]
        [MaxLength(100)]
        [Display(Name = "Логин")]
        public string UserName { get; set; } = "";

        [Required]
        [MaxLength(100)]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = "";
    }
}
