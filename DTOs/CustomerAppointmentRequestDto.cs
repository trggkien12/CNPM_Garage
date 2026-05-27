using System;
using System.ComponentModel.DataAnnotations;

namespace AutoGarageManager.DTOs
{
    public class CustomerAppointmentRequestDto
    {
        [StringLength(150)]
        public string? CustomerName { get; set; }

        [StringLength(150)]
        public string? CustomerAccount { get; set; }

        [StringLength(150)]
        public string? CustomerEmail { get; set; }

        [StringLength(150)]
        public string? Type { get; set; }

        [StringLength(250)]
        public string? ServiceName { get; set; }

        public decimal? EstimatedAmount { get; set; }

        [StringLength(100)]
        public string? SelectedTarget { get; set; }

        public int? CarId { get; set; }

        public DateTime? AppointmentDate { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }
    }
}
