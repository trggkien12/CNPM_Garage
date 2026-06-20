using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutoGarageManager.DTOs
{
    public class SelectedAppointmentServiceDto
    {
        public int? ServiceId { get; set; }
        public string? ServiceName { get; set; }
        public decimal? Price { get; set; }
    }

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

        public List<SelectedAppointmentServiceDto>? Services { get; set; }

        [StringLength(100)]
        public string? SelectedTarget { get; set; }

        public int? CarId { get; set; }

        public DateTime? AppointmentDate { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }
    }
}
