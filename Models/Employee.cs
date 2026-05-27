using System.ComponentModel.DataAnnotations;

namespace AutoGarageManager.Models
{
    public class Employee
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên nhân viên")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Số điện thoại không đúng định dạng")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        public string Position { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Lương không được âm")]
        public decimal Salary { get; set; }
    }
}
