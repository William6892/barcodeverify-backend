using System.ComponentModel.DataAnnotations;

namespace BarcodeShippingSystem.Models
{
    public class TransportCompany
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relaciones
        public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    }
}