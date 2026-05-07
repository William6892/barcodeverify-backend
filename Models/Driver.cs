using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarcodeShippingSystem.Models
{
    public class Driver
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El número de cédula es requerido")]
        [StringLength(20)]
        public string IdentificationNumber { get; set; } = string.Empty; // Cédula / Documento

        [Required(ErrorMessage = "El nombre completo es requerido")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

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