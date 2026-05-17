using System.ComponentModel.DataAnnotations;

namespace AutoGarageManager.Models
{
    public class Supplier
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty; // Gán giá trị mặc định để hết cảnh báo
        
        public string? Phone { get; set; } // Thêm dấu ? vì SĐT có thể khách không nhập (để null)
        
        public string? Address { get; set; } // Địa chỉ cũng có thể để null
        
        public string Status { get; set; } = "Hoạt động";
    }
}