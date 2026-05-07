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
        public string PlateNumber { get; set; } = string.Empty; // Placa principal

        [StringLength(20)]
        public string? TrailerPlate { get; set; } // Placa del trailer (si aplica)

        [StringLength(30)]
        public string? VehicleType { get; set; } // "Furgón", "Mula", "Camión", "Trailer", etc.

        public int TransportCompanyId { get; set; }

        [ForeignKey("TransportCompanyId")]
        public virtual TransportCompany TransportCompany { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Relación con envíos
        public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    }
}