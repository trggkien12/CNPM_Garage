using System;

namespace AutoGarageManager.DTOs
{
    public class CreateCustomerAppointmentDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAccount { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public int? CustomerId { get; set; }
        public int? CarId { get; set; }
        public string? CarPlate { get; set; }
        public string? CarName { get; set; }
        public string? Type { get; set; }
        public string? CarService { get; set; }
        public string? SelectedTarget { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string? Note { get; set; }
        public decimal? EstimatedAmount { get; set; }
    }
}
