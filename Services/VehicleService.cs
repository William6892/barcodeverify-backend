using Microsoft.EntityFrameworkCore;
using BarcodeShippingSystem.Data;
using BarcodeShippingSystem.Models;
using BarcodeShippingSystem.DTOs;

namespace BarcodeShippingSystem.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly ApplicationDbContext _context;

        public VehicleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<object> GetByCompanyAsync(int companyId)
        {
            var vehicles = await _context.VehicleTransportCompanies
                .Where(vtc => vtc.TransportCompanyId == companyId && vtc.IsActive)
                .Include(vtc => vtc.Vehicle)
                .Select(vtc => new
                {
                    vtc.Vehicle.Id,
                    vtc.Vehicle.PlateNumber,
                    vtc.Vehicle.TrailerPlate,
                    vtc.Vehicle.VehicleType,
                    vtc.Vehicle.IsActive,
                    vtc.Vehicle.CreatedAt,
                    TransportCompanyId = companyId,
                    AssignedAt = vtc.AssignedAt,
                    DisplayText = vtc.Vehicle.TrailerPlate != null && vtc.Vehicle.VehicleType == "Mula"
                        ? $"{vtc.Vehicle.PlateNumber} + {vtc.Vehicle.TrailerPlate}"
                        : vtc.Vehicle.PlateNumber
                })
                .ToListAsync();

            return vehicles;
        }

        public async Task<object> CreateAsync(CreateVehicleDto dto)
        {
            var company = await _context.TransportCompanies.FindAsync(dto.TransportCompanyId);
            if (company == null)
                throw new Exception("Transportadora no existe");

            var existingVehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.PlateNumber == dto.PlateNumber.ToUpper());

            int vehicleId;

            if (existingVehicle != null)
            {
                vehicleId = existingVehicle.Id;

                var existingLink = await _context.VehicleTransportCompanies
                    .FirstOrDefaultAsync(vtc => vtc.VehicleId == vehicleId && vtc.TransportCompanyId == dto.TransportCompanyId);

                if (existingLink != null)
                    throw new Exception("Este vehículo ya está vinculado a esta transportadora");
            }
            else
            {
                var vehicle = new Vehicle
                {
                    PlateNumber = dto.PlateNumber.ToUpper(),
                    TrailerPlate = dto.TrailerPlate?.ToUpper(),
                    VehicleType = dto.VehicleType,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Vehicles.Add(vehicle);
                await _context.SaveChangesAsync();
                vehicleId = vehicle.Id;
            }

            var vehicleCompany = new VehicleTransportCompany
            {
                VehicleId = vehicleId,
                TransportCompanyId = dto.TransportCompanyId,
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.VehicleTransportCompanies.Add(vehicleCompany);
            await _context.SaveChangesAsync();

            return new
            {
                message = existingVehicle != null ? "Vehículo vinculado exitosamente" : "Vehículo creado y vinculado exitosamente",
                vehicleId = vehicleId,
                plateNumber = dto.PlateNumber.ToUpper(),
                trailerPlate = dto.TrailerPlate?.ToUpper(),
                vehicleType = dto.VehicleType,
                transportCompanyId = dto.TransportCompanyId
            };
        }
    }
}