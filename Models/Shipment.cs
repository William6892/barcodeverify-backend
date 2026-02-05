// Models/Shipment.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarcodeShippingSystem.Models
{
    public class Shipment
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ShipmentNumber { get; set; } = string.Empty;

        [Required]
        public int TransportCompanyId { get; set; }

        [Required]
        public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed, Cancelled

        // AGREGA ESTA LÍNEA:
        public DateTime? StartedAt { get; set; }  // Fecha cuando comenzó el escaneo

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedByUserId { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime? EstimatedDeparture { get; set; }

        public DateTime? ActualDeparture { get; set; }

        // Relaciones
        public TransportCompany? TransportCompany { get; set; }
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<ScanOperation> ScanOperations { get; set; } = new List<ScanOperation>();
        public User? CreatedByUser { get; set; }
    }
}