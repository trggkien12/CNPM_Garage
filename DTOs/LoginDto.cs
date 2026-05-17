using System.ComponentModel.DataAnnotations;

namespace AutoGarageManager.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Vui lòng nhập tài khoản hoặc số điện thoại")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;
    }
}
