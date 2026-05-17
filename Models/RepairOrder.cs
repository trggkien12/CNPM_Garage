using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AutoGarageManager.Models
{
    public class RepairOrder
    {
        public int RepairOrderId { get; set; }

        public int CarId { get; set; }

        public DateTime RepairDate { get; set; } = DateTime.Now;

        public string Status { get; set; } = "Chờ xử lý";

        [JsonIgnore]
        public Car? Car { get; set; }

        [JsonIgnore]
        public ICollection<RepairDetail> RepairDetails { get; set; } = new List<RepairDetail>();
    }
}
