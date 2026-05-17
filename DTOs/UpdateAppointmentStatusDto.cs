using System.ComponentModel.DataAnnotations;

namespace AutoGarageManager.DTOs
{
    public class UpdateAppointmentStatusDto
    {
        [Required]
        [RegularExpression("^(Chờ xác nhận|Đã xác nhận|Đã từ chối|Đã hủy|Đã chuyển phiếu sửa)$", ErrorMessage = "Trạng thái lịch hẹn không hợp lệ")]
        public string Status { get; set; } = "Chờ xác nhận";

        [StringLength(500, ErrorMessage = "Lý do từ chối tối đa 500 ký tự")]
        public string? RejectionReason { get; set; }
    }
}
