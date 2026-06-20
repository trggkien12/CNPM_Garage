using System.ComponentModel.DataAnnotations;

namespace AutoGarageManager.DTOs
{
    public class CreateRepairOrderDTO
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Mã xe không hợp lệ")]
        public int CarId { get; set; }

        [RegularExpression("^(Chờ xử lý|Chờ sửa|Đang sửa|Chờ phụ tùng|Hoàn thành|Đã giao xe|Đã hủy)$", ErrorMessage = "Trạng thái phiếu sửa không hợp lệ")]
        public string Status { get; set; } = "Chờ xử lý";

        [StringLength(1000)] public string? ProblemDescription { get; set; }
        [StringLength(1000)] public string? VehicleCondition { get; set; }
        [StringLength(1000)] public string? Diagnosis { get; set; }
        [StringLength(1000)] public string? TechnicalNote { get; set; }
        [StringLength(150)] public string? AssignedEmployee { get; set; }
        [StringLength(150)] public string? TechnicianName { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public DateTime? EstimatedCompletionDate { get; set; }
    }
}
