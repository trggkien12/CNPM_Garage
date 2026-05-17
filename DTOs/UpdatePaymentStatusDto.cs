using System.ComponentModel.DataAnnotations;

namespace AutoGarageManager.DTOs
{
    public class UpdatePaymentStatusDto
    {
        [StringLength(100)]
        public string? ConfirmedBy { get; set; }

        [StringLength(250)]
        public string? Note { get; set; }
    }
}
