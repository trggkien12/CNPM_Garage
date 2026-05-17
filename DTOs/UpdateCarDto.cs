using System.ComponentModel.DataAnnotations;

namespace AutoGarageManager.DTOs
{
    public class UpdateCarDto
    {
        [Required]
        [StringLength(20, MinimumLength = 2)]
        public string LicensePlate { get; set; } = string.Empty; // Thêm = string.Empty;

        [Required]
        [StringLength(50)]
        public string Brand { get; set; } = string.Empty; // Thêm = string.Empty;

        [Required]
        [StringLength(50)]
        public string Model { get; set; } = string.Empty; // Thêm = string.Empty;

        [Range(1900, 2100)]
        public int Year { get; set; }

        [Required]
        public int CustomerId { get; set; }
    }
}