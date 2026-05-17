using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AutoGarageManager.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Mã hóa đơn không hợp lệ")]
        public int InvoiceId { get; set; }

        [Range(1, double.MaxValue, ErrorMessage = "Số tiền thanh toán phải lớn hơn 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Tiền mặt";

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Chờ xác nhận";

        [StringLength(250)]
        public string? Note { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        public DateTime? ConfirmedAt { get; set; }

        [StringLength(100)]
        public string? ConfirmedBy { get; set; }

        [JsonIgnore]
        public Invoice? Invoice { get; set; }
    }
}
