using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarcodeShippingSystem.Models
{
    public class Vehicle
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "La placa del vehículo es requerida")]
        [StringLength(20)]
        public string PlateNumber { get; set; } = string.Empty;

        [StringLength(20)]
        public string? TrailerPlate { get; set; }

        [StringLength(30)]
        public string? VehicleType { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // ✅ Relación muchos a muchos (tabla puente)
        public virtual ICollection<VehicleTransportCompany> VehicleTransportCompanies { get; set; } = new List<VehicleTransportCompany>();

        // Relación con envíos
        public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    }
}