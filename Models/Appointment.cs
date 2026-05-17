using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AutoGarageManager.Models
{
    public class Appointment
    {
        [Key]
        public int AppointmentId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        public int? CarId { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        [Required]
        public string Status { get; set; } = "Chờ xác nhận";

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        public DateTime? RejectedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [JsonIgnore]
        public Customer? Customer { get; set; }

        [JsonIgnore]
        public Car? Car { get; set; }
    }
}
