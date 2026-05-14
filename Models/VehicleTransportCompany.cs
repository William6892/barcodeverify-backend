using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarcodeShippingSystem.Models
{
    public class VehicleTransportCompany
    {
        [Key]
        public int Id { get; set; }

        public int VehicleId { get; set; }

        [ForeignKey("VehicleId")]
        public virtual Vehicle Vehicle { get; set; } = null!;

        public int TransportCompanyId { get; set; }

        [ForeignKey("TransportCompanyId")]
        public virtual TransportCompany TransportCompany { get; set; } = null!;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
    }
}