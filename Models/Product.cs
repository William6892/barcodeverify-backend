using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarcodeShippingSystem.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Barcode { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;

        public int Quantity { get; set; } = 1;

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;  // Televisores, Monitores, Celulares, Tablets

        [StringLength(50)]
        public string Brand { get; set; } = "Samsung";  // Siempre Samsung

        [StringLength(50)]
        public string? Model { get; set; }  // Modelo específico: QN90B, S24 Ultra, etc.

        [StringLength(100)]
        public string? SerialNumber { get; set; }  // Número de serie (opcional)
        
        public int? ShipmentId { get; set; }

        [Required]
        public DateTime ScannedAt { get; set; } = DateTime.UtcNow;

        public int? ScannedByUserId { get; set; }

        // Relaciones
        public Shipment? Shipment { get; set; }
        public User? ScannedByUser { get; set; }
    }
}