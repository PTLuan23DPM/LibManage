using System.ComponentModel.DataAnnotations;

namespace LibWeb.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username required")]
        [StringLength(50)]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password required")]
        [StringLength(20, MinimumLength = 6, ErrorMessage = "6-20 characters password")]
        [DataType(DataType.Password)]
        public string PasswordHash { get; set; }
    }
}
