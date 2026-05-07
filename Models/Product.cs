// Models/Product.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarcodeShippingSystem.Models
{
    public class Product
    {
        [Key]
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

        [Range(1, 999999)]
        public int Quantity { get; set; } = 1;

        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Brand { get; set; }

        [StringLength(50)]
        public string? Model { get; set; }

        [StringLength(100)]
        public string? SerialNumber { get; set; }

        public int? ShipmentId { get; set; }

        [ForeignKey("ShipmentId")]
        public virtual Shipment? Shipment { get; set; }

        public DateTime ScannedAt { get; set; } = DateTime.UtcNow;

        // ✅ Propiedad existente (o agregar si no está)
        public int? ScannedByUserId { get; set; }

        // ✅ AGREGAR ESTA PROPIEDAD DE NAVEGACIÓN
        [ForeignKey("ScannedByUserId")]
        public virtual User? ScannedByUser { get; set; }
    }
}