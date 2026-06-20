using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AutoGarageManager.Models
{
    public class RepairOrder
    {
        public int RepairOrderId { get; set; }
        public int CarId { get; set; }
        public DateTime RepairDate { get; set; } = DateTime.Now;
        public DateTime ReceivedDate { get; set; } = DateTime.Now;
        public DateTime? EstimatedCompletionDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        [StringLength(1000)]
        public string ProblemDescription { get; set; } = string.Empty;

        [StringLength(1000)]
        public string VehicleCondition { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Diagnosis { get; set; } = string.Empty;

        [StringLength(1000)]
        public string TechnicalNote { get; set; } = string.Empty;

        [StringLength(150)]
        public string AssignedEmployee { get; set; } = string.Empty;

        [StringLength(150)]
        public string TechnicianName { get; set; } = string.Empty;

        public string Status { get; set; } = "Chờ xử lý";

        [JsonIgnore]
        public Car? Car { get; set; }

        [JsonIgnore]
        public ICollection<RepairDetail> RepairDetails { get; set; } = new List<RepairDetail>();
    }
}
