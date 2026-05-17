using System.ComponentModel.DataAnnotations;

namespace AutoGarageManager.DTOs
{
    public class CreateRepairOrderDTO
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Mã xe không hợp lệ")]
        public int CarId { get; set; }

        [Required]
        [RegularExpression("^(Chờ xử lý|Đang sửa|Hoàn thành|Đã hủy)$", ErrorMessage = "Trạng thái phiếu sửa không hợp lệ")]
        public string Status { get; set; } = "Chờ xử lý";
    }
}
