using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarcodeShippingSystem.Models
{
    [Table("InventoryTransactions")]
    public class InventoryTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [StringLength(10)]
        public string Type { get; set; } = string.Empty; // "IN" o "OUT"

        [Required]
        public int Quantity { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? ReferenceId { get; set; }

        [StringLength(50)]
        public string? ReferenceType { get; set; }

        public int? UserId { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }
}