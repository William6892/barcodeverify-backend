using Microsoft.EntityFrameworkCore;
using BarcodeShippingSystem.Data;
using BarcodeShippingSystem.Models;
using BarcodeShippingSystem.DTOs;

namespace BarcodeShippingSystem.Services
{
    public class DriverService : IDriverService
    {
        private readonly ApplicationDbContext _context;

        public DriverService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ Obtener conductores VINCULADOS a una transportadora
        public async Task<object> GetByCompanyAsync(int companyId)
        {
            var drivers = await _context.DriverTransportCompanies
                .Where(dtc => dtc.TransportCompanyId == companyId && dtc.IsActive)
                .Include(dtc => dtc.Driver)
                .Select(dtc => new
                {
                    dtc.Driver.Id,
                    dtc.Driver.IdentificationNumber,
                    dtc.Driver.FullName,
                    dtc.Driver.IsActive,
                    dtc.Driver.CreatedAt,
                    LinkedAt = dtc.AssignedAt
                })
                .ToListAsync();

            return drivers;
        }

        // ✅ Crear conductor INDEPENDIENTE (sin transportadora)
        public async Task<object> CreateAsync(CreateDriverDto dto)
        {
            // Verificar si ya existe por cédula
            var existingDriver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.IdentificationNumber == dto.IdentificationNumber);

            if (existingDriver != null)
                throw new Exception("Ya existe un conductor con esta cédula");

            var driver = new Driver
            {
                IdentificationNumber = dto.IdentificationNumber,
                FullName = dto.FullName.ToUpper(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();

            // Si viene con transportadora, lo vinculamos
            if (dto.TransportCompanyId > 0)
            {
                var link = new DriverTransportCompany
                {
                    DriverId = driver.Id,
                    TransportCompanyId = dto.TransportCompanyId,
                    AssignedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _context.DriverTransportCompanies.Add(link);
                await _context.SaveChangesAsync();
            }

            return new
            {
                message = "Conductor creado exitosamente",
                driver = new { driver.Id, driver.IdentificationNumber, driver.FullName }
            };
        }

        // ✅ Vincular conductor EXISTENTE a una transportadora
        public async Task<object> LinkToCompanyAsync(int driverId, int transportCompanyId)
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver == null)
                throw new Exception("Conductor no encontrado");

            var company = await _context.TransportCompanies.FindAsync(transportCompanyId);
            if (company == null)
                throw new Exception("Transportadora no encontrada");

            var existingLink = await _context.DriverTransportCompanies
                .FirstOrDefaultAsync(dtc => dtc.DriverId == driverId && dtc.TransportCompanyId == transportCompanyId);

            if (existingLink != null)
                throw new Exception("Este conductor ya está vinculado a esta transportadora");

            var link = new DriverTransportCompany
            {
                DriverId = driverId,
                TransportCompanyId = transportCompanyId,
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.DriverTransportCompanies.Add(link);
            await _context.SaveChangesAsync();

            return new
            {
                message = "Conductor vinculado a la transportadora exitosamente",
                driverId = driverId,
                transportCompanyId = transportCompanyId
            };
        }

        // ✅ Obtener conductores NO vinculados a una transportadora específica
        public async Task<object> GetUnlinkedDriversAsync()
        {
            var allDrivers = await _context.Drivers
                .Where(d => d.IsActive)
                .Select(d => new
                {
                    d.Id,
                    d.IdentificationNumber,
                    d.FullName
                })
                .ToListAsync();

            return allDrivers;
        }
    }
}