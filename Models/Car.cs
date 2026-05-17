using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AutoGarageManager.Models
{
    public class Car
    {
        public int CarId { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 2)]
        public string LicensePlate { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Brand { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Model { get; set; } = string.Empty;

        [Range(1900, 2100)]
        public int Year { get; set; }

        [Range(1, int.MaxValue)]
        public int CustomerId { get; set; }

        [JsonIgnore]
        public Customer? Customer { get; set; }
    }
}
