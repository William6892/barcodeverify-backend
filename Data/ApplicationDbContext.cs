using Microsoft.EntityFrameworkCore;
using BarcodeShippingSystem.Models;

namespace BarcodeShippingSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<TransportCompany> TransportCompanies { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ScanOperation> ScanOperations { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<DriverTransportCompany> DriverTransportCompanies { get; set; }
        public DbSet<VehicleTransportCompany> VehicleTransportCompanies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==================== USER ====================
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.Property(u => u.Username)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(u => u.Email)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(u => u.PasswordHash)
                    .IsRequired();

                entity.Property(u => u.Role)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("User");

                entity.Property(u => u.IsActive)
                    .HasDefaultValue(true);

                entity.Property(u => u.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // ❌ ELIMINADA la configuración de CompletedShipments
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
            });

            // ==================== TRANSPORT COMPANY ====================
            modelBuilder.Entity<TransportCompany>(entity =>
            {
                entity.HasKey(tc => tc.Id);

                entity.Property(tc => tc.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(tc => tc.IsActive)
                    .HasDefaultValue(true);

                entity.Property(tc => tc.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(tc => tc.Name);
            });

            // ==================== DRIVER ====================
            modelBuilder.Entity<Driver>(entity =>
            {
                entity.HasKey(d => d.Id);

                entity.Property(d => d.IdentificationNumber)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(d => d.FullName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(d => d.IsActive)
                    .HasDefaultValue(true);

                entity.Property(d => d.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(d => d.IdentificationNumber)
                    .IsUnique()
                    .HasFilter("\"IsActive\" = true");
            });

            // ==================== VEHICLE ====================
            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasKey(v => v.Id);

                entity.Property(v => v.PlateNumber)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(v => v.TrailerPlate)
                    .HasMaxLength(20);

                entity.Property(v => v.VehicleType)
                    .HasMaxLength(30);

                entity.Property(v => v.IsActive)
                    .HasDefaultValue(true);

                entity.Property(v => v.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(v => v.PlateNumber)
                    .IsUnique()
                    .HasFilter("\"IsActive\" = true");
            });

            // ==================== SHIPMENT ====================
            modelBuilder.Entity<Shipment>(entity =>
            {
                entity.HasKey(s => s.Id);

                entity.Property(s => s.ShipmentNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(s => s.Status)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Pending");

                entity.Property(s => s.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(s => s.Notes)
                    .HasMaxLength(500);

                entity.HasOne(s => s.TransportCompany)
                    .WithMany(tc => tc.Shipments)
                    .HasForeignKey(s => s.TransportCompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Driver)
                    .WithMany(d => d.Shipments)
                    .HasForeignKey(s => s.DriverId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Vehicle)
                    .WithMany(v => v.Shipments)
                    .HasForeignKey(s => s.VehicleId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.CreatedBy)
                    .WithMany(u => u.Shipments)
                    .HasForeignKey(s => s.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // ❌ ELIMINADA la configuración de CompletedBy

                entity.HasIndex(s => s.ShipmentNumber).IsUnique();
                entity.HasIndex(s => s.Status);
                entity.HasIndex(s => s.CreatedAt);
            });

            // ==================== PRODUCT ====================
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Barcode)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(p => p.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(p => p.Description)
                    .HasMaxLength(500);

                entity.Property(p => p.SKU)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(p => p.Category)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(p => p.Brand)
                    .HasMaxLength(50);

                entity.Property(p => p.Model)
                    .HasMaxLength(50);

                entity.Property(p => p.SerialNumber)
                    .HasMaxLength(100);

                entity.Property(p => p.Quantity)
                    .HasDefaultValue(1);

                entity.Property(p => p.ScannedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(p => p.Shipment)
                    .WithMany(s => s.Products)
                    .HasForeignKey(p => p.ShipmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.ScannedByUser)
                    .WithMany(u => u.ScannedProducts)
                    .HasForeignKey(p => p.ScannedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(p => new { p.Barcode, p.ShipmentId });
                entity.HasIndex(p => p.Barcode);

                entity.HasIndex(p => p.SerialNumber)
                    .HasFilter("\"SerialNumber\" IS NOT NULL");

                entity.HasIndex(p => new { p.SerialNumber, p.ShipmentId })
                    .IsUnique()
                    .HasFilter("\"SerialNumber\" IS NOT NULL");
            });

            // ==================== SCAN OPERATION ====================
            modelBuilder.Entity<ScanOperation>(entity =>
            {
                entity.HasKey(so => so.Id);

                entity.Property(so => so.Status)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Active");

                entity.Property(so => so.StartTime)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(so => so.ProductCount)
                    .HasDefaultValue(0);

                entity.HasOne(so => so.Shipment)
                    .WithMany(s => s.ScanOperations)
                    .HasForeignKey(so => so.ShipmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(so => so.User)
                    .WithMany(u => u.ScanOperations)
                    .HasForeignKey(so => so.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(so => so.Status);
                entity.HasIndex(so => so.StartTime);
            });

            // Configuración de DriverTransportCompany
            modelBuilder.Entity<DriverTransportCompany>(entity =>
            {
                entity.HasKey(dtc => dtc.Id);

                entity.HasOne(dtc => dtc.Driver)
                    .WithMany(d => d.DriverTransportCompanies)
                    .HasForeignKey(dtc => dtc.DriverId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(dtc => dtc.TransportCompany)
                    .WithMany(tc => tc.DriverTransportCompanies)
                    .HasForeignKey(dtc => dtc.TransportCompanyId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(dtc => new { dtc.DriverId, dtc.TransportCompanyId })
                    .IsUnique();
            });

            // Configuración de VehicleTransportCompany
            modelBuilder.Entity<VehicleTransportCompany>(entity =>
            {
                entity.HasKey(vtc => vtc.Id);

                entity.HasOne(vtc => vtc.Vehicle)
                    .WithMany(v => v.VehicleTransportCompanies)
                    .HasForeignKey(vtc => vtc.VehicleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(vtc => vtc.TransportCompany)
                    .WithMany(tc => tc.VehicleTransportCompanies)
                    .HasForeignKey(vtc => vtc.TransportCompanyId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(vtc => new { vtc.VehicleId, vtc.TransportCompanyId })
                    .IsUnique();
            });
        }
    }
}