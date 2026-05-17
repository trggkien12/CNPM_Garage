using System.ComponentModel.DataAnnotations;

namespace AutoGarageManager.Models
{
    public class Customer
    {
        [Key]
        public int Id { get; set; } 
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}