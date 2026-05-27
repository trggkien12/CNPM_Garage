using System.ComponentModel.DataAnnotations;

namespace AutoGarageManager.DTOs
{
    public class RejectPaymentDto
    {
        [Required(ErrorMessage = "Vui lòng nhập lý do từ chối")]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ConfirmedBy { get; set; }
    }
}
