using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarcodeShippingSystem.Models
{
    public class DriverTransportCompany
    {
        [Key]
        public int Id { get; set; }

        public int DriverId { get; set; }

        [ForeignKey("DriverId")]
        public virtual Driver Driver { get; set; } = null!;

        public int TransportCompanyId { get; set; }

        [ForeignKey("TransportCompanyId")]
        public virtual TransportCompany TransportCompany { get; set; } = null!;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
    }
}