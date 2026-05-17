using System.Text.Json.Serialization;

namespace AutoGarageManager.Models
{
    public class RepairDetail
    {
        public int RepairDetailId { get; set; }

        public int RepairOrderId { get; set; }

        public int ServiceId { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }

        [JsonIgnore]
        public RepairOrder RepairOrder { get; set; }

        public Service Service { get; set; }
    }
}