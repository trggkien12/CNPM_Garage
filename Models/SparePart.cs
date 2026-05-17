using System.ComponentModel.DataAnnotations;

namespace AutoGarageManager.Models
{
    public class SparePart
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên phụ tùng")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Giá phụ tùng không được âm")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn không được âm")]
        public int StockQuantity { get; set; }
    }
}
