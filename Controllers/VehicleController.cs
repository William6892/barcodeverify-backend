using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BarcodeShippingSystem.Data;
using BarcodeShippingSystem.Models;

namespace BarcodeShippingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VehicleController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public VehicleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Vehicle/company/{companyId}
        [HttpGet("company/{companyId}")]
        public async Task<IActionResult> GetByCompany(int companyId)
        {
            // ✅ Buscar en la tabla puente
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

            return Ok(vehicles);
        }

        // POST: api/Vehicle - CREAR vehículo y VINCULAR a transportadora
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVehicleDto dto)
        {
            // Validar que la transportadora existe
            var company = await _context.TransportCompanies.FindAsync(dto.TransportCompanyId);
            if (company == null)
                return BadRequest(new { message = "Transportadora no existe" });

            // Verificar si el vehículo ya existe por placa
            var existingVehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.PlateNumber == dto.PlateNumber.ToUpper());

            int vehicleId;

            if (existingVehicle != null)
            {
                vehicleId = existingVehicle.Id;

                // Verificar si ya está vinculado a esta transportadora
                var existingLink = await _context.VehicleTransportCompanies
                    .FirstOrDefaultAsync(vtc => vtc.VehicleId == vehicleId && vtc.TransportCompanyId == dto.TransportCompanyId);

                if (existingLink != null)
                    return Conflict(new { message = "Este vehículo ya está vinculado a esta transportadora" });
            }
            else
            {
                // Crear nuevo vehículo
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

            // Crear el vínculo con la transportadora
            var vehicleCompany = new VehicleTransportCompany
            {
                VehicleId = vehicleId,
                TransportCompanyId = dto.TransportCompanyId,
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.VehicleTransportCompanies.Add(vehicleCompany);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = existingVehicle != null ? "Vehículo vinculado exitosamente" : "Vehículo creado y vinculado exitosamente",
                vehicleId = vehicleId,
                plateNumber = dto.PlateNumber.ToUpper(),
                trailerPlate = dto.TrailerPlate?.ToUpper(),
                vehicleType = dto.VehicleType,
                transportCompanyId = dto.TransportCompanyId
            });
        }
    }

    public class CreateVehicleDto
    {
        public string PlateNumber { get; set; } = string.Empty;
        public string? TrailerPlate { get; set; }
        public string? VehicleType { get; set; }
        public int TransportCompanyId { get; set; }
    }
}