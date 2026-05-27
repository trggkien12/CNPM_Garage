using System;
using System.ComponentModel.DataAnnotations;

namespace AutoGarageManager.DTOs
{
    public class QrPaymentRequestDto
    {
        public int? InvoiceId { get; set; }

        [StringLength(100)]
        public string? LocalOrderId { get; set; }

        [StringLength(150)]
        public string? CustomerName { get; set; }

        [StringLength(150)]
        public string? CustomerAccount { get; set; }

        [StringLength(150)]
        public string? CustomerEmail { get; set; }

        [StringLength(250)]
        public string? ServiceName { get; set; }

        [Range(1, double.MaxValue, ErrorMessage = "Số tiền thanh toán phải lớn hơn 0")]
        public decimal Amount { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }
    }
}
