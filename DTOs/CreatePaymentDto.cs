using System.ComponentModel.DataAnnotations;

namespace AutoGarageManager.DTOs
{
    public class CreatePaymentDto
    {
        [Required(ErrorMessage = "Vui lòng nhập mã hóa đơn")]
        [Range(1, int.MaxValue, ErrorMessage = "Mã hóa đơn không hợp lệ")]
        public int InvoiceId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số tiền thanh toán")]
        [Range(1, double.MaxValue, ErrorMessage = "Số tiền thanh toán phải lớn hơn 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Tiền mặt";

        [StringLength(250)]
        public string? Note { get; set; }
    }
}
