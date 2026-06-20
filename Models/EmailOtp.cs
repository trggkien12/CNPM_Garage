using System.ComponentModel.DataAnnotations;

namespace AutoGarageManager.Models
{
    public class EmailOtp
    {
        [Key]
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty; // REGISTER hoặc FORGOT_PASSWORD
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
