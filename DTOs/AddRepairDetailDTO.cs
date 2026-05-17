using System.ComponentModel.DataAnnotations;

namespace AutoGarageManager.DTOs
{
    public class AddRepairDetailDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Mã phiếu sửa không hợp lệ")]
        public int RepairOrderId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Mã dịch vụ không hợp lệ")]
        public int ServiceId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá tiền không được âm")]
        public decimal Price { get; set; } 
    }
}
