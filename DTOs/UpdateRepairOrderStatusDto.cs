using System.ComponentModel.DataAnnotations;

namespace AutoGarageManager.DTOs
{
    public class UpdateRepairOrderStatusDto
    {
        [Required(ErrorMessage = "Vui lòng nhập trạng thái")]
        [RegularExpression("^(Chờ xử lý|Đang sửa|Hoàn thành|Đã hủy)$", ErrorMessage = "Trạng thái phiếu sửa không hợp lệ")]
        public string Status { get; set; } = "Chờ xử lý";
    }
}
