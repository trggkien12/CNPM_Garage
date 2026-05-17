using System.ComponentModel.DataAnnotations;

namespace AutoGarageManager.DTOs
{
    public class CreateAppointmentDto
    {
        [Required(ErrorMessage = "Vui lòng nhập mã khách hàng")]
        [Range(1, int.MaxValue, ErrorMessage = "Mã khách hàng không hợp lệ")]
        public int CustomerId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Mã xe không hợp lệ")]
        public int? CarId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thời gian hẹn")]
        public DateTime AppointmentDate { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự")]
        public string? Note { get; set; }
    }
}
