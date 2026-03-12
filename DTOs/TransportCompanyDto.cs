using System.ComponentModel.DataAnnotations;
namespace BarcodeShippingSystem.DTOs
{
    public class TransportCompanyDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class CreateTransportCompanyDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
    }
    public class UpdateTransportCompanyDto
    {
        [StringLength(200)]
        public string? Name { get; set; }
        public bool? IsActive { get; set; }
    }
}