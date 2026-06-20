using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AutoGarageManager.Models
{
    public class Warranty
    {
        [Key]
        public int Id { get; set; }

        public int? CustomerId { get; set; }
        public int? CarId { get; set; }
        public int? ServiceId { get; set; }
        public int? SparePartId { get; set; }
        public int? InvoiceId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập dịch vụ bảo hành")]
        [StringLength(200)]
        public string ServiceName { get; set; } = string.Empty;

        [StringLength(150)]
        public string CustomerName { get; set; } = string.Empty;

        public DateTime PurchaseDate { get; set; } = DateTime.Now;
        public DateTime ExpiryDate { get; set; } = DateTime.Now.AddMonths(3);

        [StringLength(50)]
        public string Status { get; set; } = "Còn hạn";

        [JsonIgnore] public Customer? Customer { get; set; }
        [JsonIgnore] public Car? Car { get; set; }
        [JsonIgnore] public Service? Service { get; set; }
        [JsonIgnore] public SparePart? SparePart { get; set; }
        [JsonIgnore] public Invoice? Invoice { get; set; }
    }
}
