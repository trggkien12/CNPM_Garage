using System.ComponentModel.DataAnnotations;

namespace AutoGarageManager.DTOs
{
    public class UpdateCustomerDto
    {
        [Required]
        [StringLength(150)]
        public string FullName { get; set; }

        [Required]
        [Phone]
        [StringLength(30)]
        public string PhoneNumber { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(250)]
        public string Address { get; set; }
    }
}
