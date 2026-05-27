using System.ComponentModel.DataAnnotations;

namespace AutoGarageManager.Models
{
    public class Warranty
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập dịch vụ bảo hành")]
        [StringLength(200)]
        public string ServiceName { get; set; } = string.Empty;

        [StringLength(150)]
        public string CustomerName { get; set; } = string.Empty;

        [StringLength(50)]
        public string PurchaseDate { get; set; } = string.Empty;

        [StringLength(50)]
        public string ExpiryDate { get; set; } = string.Empty;
    }
}
