using BarcodeShippingSystem.Models;

namespace BarcodeShippingSystem.DTOs
{
    public class BarcodeShippingSystem
    {
        // DTOs auxiliares
        public class TransportCompanyDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string DriverName { get; set; } = string.Empty;
            public string LicensePlate { get; set; } = string.Empty;
            public bool IsActive { get; set; }
        }

        public class UserDto
        {
            public int Id { get; set; }
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
        }
        public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
        public virtual ICollection<Product> ScannedProducts { get; set; } = new List<Product>();
        public virtual ICollection<ScanOperation> ScanOperations { get; set; } = new List<ScanOperation>();
    }
}
