using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AutoGarageManager.Models
{
    public class RepairPart
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RepairOrderId { get; set; }

        [Required]
        public int SparePartId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        [ForeignKey("RepairOrderId")]
        [JsonIgnore]
        public RepairOrder? RepairOrder { get; set; }

        [ForeignKey("SparePartId")]
        [JsonIgnore]
        public SparePart? SparePart { get; set; }
    }
}