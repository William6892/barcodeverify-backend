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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurar índices únicos
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Product>()
       .HasIndex(p => new { p.Barcode, p.ShipmentId })
       .IsUnique(false);

            modelBuilder.Entity<Shipment>()
                .HasIndex(s => s.ShipmentNumber)
                .IsUnique();

            modelBuilder.Entity<TransportCompany>()
                .HasIndex(tc => tc.LicensePlate)
                .IsUnique();
            modelBuilder.Entity<Product>()
        .HasIndex(p => new { p.SerialNumber, p.ShipmentId })
        .IsUnique()
        .HasFilter("[SerialNumber] IS NOT NULL");
            modelBuilder.Entity<Product>()
        .HasIndex(p => p.Barcode);

            modelBuilder.Entity<Product>()
        .HasIndex(p => p.SerialNumber)
        .HasFilter("[SerialNumber] IS NOT NULL");
        }
    }
}