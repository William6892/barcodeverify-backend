using System.ComponentModel.DataAnnotations;

namespace BarcodeShippingSystem.Models
{
    public class ScanOperation
    {
        public int Id { get; set; }

        [Required]
        public int ShipmentId { get; set; }

        [Required]
        public int UserId { get; set; }

        public int ProductCount { get; set; }

        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        public DateTime? EndTime { get; set; }

        [StringLength(50)]
        public string? Status { get; set; } = "Active"; // Active, Completed, Cancelled

        // Relaciones
        public Shipment? Shipment { get; set; }
        public User? User { get; set; }
    }
}