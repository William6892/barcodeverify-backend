using System.ComponentModel.DataAnnotations;

namespace BarcodeShippingSystem.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = "User";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLogin { get; set; }

        // ✅ RELACIONES CORRECTAS
        public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
        public ICollection<Product> ScannedProducts { get; set; } = new List<Product>();
        public ICollection<ScanOperation> ScanOperations { get; set; } = new List<ScanOperation>();
        
    }
}