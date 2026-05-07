using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarcodeShippingSystem.Models
{
    public class Shipment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ShipmentNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public int? TransportCompanyId { get; set; }
        [ForeignKey("TransportCompanyId")]
        public virtual TransportCompany? TransportCompany { get; set; }

        public int? DriverId { get; set; }
        [ForeignKey("DriverId")]
        public virtual Driver? Driver { get; set; }

        public int? VehicleId { get; set; }
        [ForeignKey("VehicleId")]
        public virtual Vehicle? Vehicle { get; set; }

        public int CreatedByUserId { get; set; }
        [ForeignKey("CreatedByUserId")]
        public virtual User? CreatedBy { get; set; }

        public int? LastModifiedByUserId { get; set; }
        public virtual User? LastModifiedBy { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? EstimatedDeparture { get; set; }
        public DateTime? ActualDeparture { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime? StartedAt { get; set; }

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<ScanOperation> ScanOperations { get; set; } = new List<ScanOperation>();

    }
}