using System.ComponentModel.DataAnnotations;

namespace AutoGarageManager.Models
{
    public class Service
    {
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên dịch vụ")]
        [StringLength(150)]
        public string ServiceName { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Giá dịch vụ không được âm")]
        public decimal Price { get; set; }

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
    }
}
