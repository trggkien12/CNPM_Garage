using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AutoGarageManager.Models
{
    public class AppointmentService
    {
        public int AppointmentServiceId { get; set; }
        public int AppointmentId { get; set; }
        public int? ServiceId { get; set; }

        [StringLength(200)]
        public string ServiceName { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [JsonIgnore]
        public Appointment? Appointment { get; set; }

        [JsonIgnore]
        public Service? Service { get; set; }
    }
}
