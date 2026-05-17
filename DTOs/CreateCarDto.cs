using System.ComponentModel.DataAnnotations;

namespace AutoGarageManager.DTOs
{
    public class CreateCarDto
    {
        [Required]
        [StringLength(20, MinimumLength = 2)]
        public string LicensePlate { get; set; }

        [Required]
        [StringLength(50)]
        public string Brand { get; set; }

        [Required]
        [StringLength(50)]
        public string Model { get; set; }

        [Range(1900, 2100)]
        public int Year { get; set; }

        [Required]
        public int CustomerId { get; set; }
    }
}
