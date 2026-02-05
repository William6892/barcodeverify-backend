using System.ComponentModel.DataAnnotations;

namespace BarcodeShippingSystem.DTOs
{
    public class TransportCompanyDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateTransportCompanyDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DriverName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string LicensePlate { get; set; } = string.Empty;
    }

    public class UpdateTransportCompanyDto
    {
        [StringLength(200)]
        public string? Name { get; set; }

        [StringLength(15)]
        [Phone]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? DriverName { get; set; }

        [StringLength(20)]
        public string? LicensePlate { get; set; }

        public bool? IsActive { get; set; }
    }
}