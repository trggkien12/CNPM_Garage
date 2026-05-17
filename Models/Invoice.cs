using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AutoGarageManager.Models
{
    public class Invoice
    {
        public int InvoiceId { get; set; }

        public int RepairOrderId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Chưa thanh toán";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [JsonIgnore]
        public RepairOrder? RepairOrder { get; set; }

        [JsonIgnore]
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
