using System.ComponentModel.DataAnnotations;

namespace AutoGarageManager.Models
{
    /// <summary>
    /// Thực thể Nhà cung cấp (vật tư, phụ tùng). Dùng để tạo bảng Supplier trong Database.
    /// </summary>
    public class Warranty
    {
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// [Required] ép DB không được để trống (NOT NULL).
        /// Việc gán "= string.Empty" là mẹo để triệt tiêu cảnh báo (Warning) của trình biên dịch C# về biến có thể bị null.
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty; 
        
        /// <summary>
        /// Kiểu dữ liệu có dấu "?" (string?) báo cho Entity Framework biết cột này cho phép giá trị NULL (Khách không nhập cũng được).
        /// </summary>
        public string? Phone { get; set; } 
        
        public string? Address { get; set; } 
        
        /// <summary>
        /// Gán sẵn giá trị mặc định. Khi thêm mới nhà cung cấp mà không truyền Status, DB sẽ tự lưu là "Hoạt động".
        /// </summary>
        public string Status { get; set; } = "Hoạt động";
    }
}