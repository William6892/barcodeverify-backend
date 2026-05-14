using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarcodeShippingSystem.Models
{
    public class TransportCompany
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ✅ Relaciones muchos a muchos (tablas puente)
        public virtual ICollection<DriverTransportCompany> DriverTransportCompanies { get; set; } = new List<DriverTransportCompany>();

        public virtual ICollection<VehicleTransportCompany> VehicleTransportCompanies { get; set; } = new List<VehicleTransportCompany>();

        // Relación con envíos
        public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    }
}