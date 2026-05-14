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
    public class DriverController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DriverController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Driver/company/{companyId}
        [HttpGet("company/{companyId}")]
        public async Task<IActionResult> GetByCompany(int companyId)
        {
            // ✅ Buscar en la tabla puente
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
                    TransportCompanyId = companyId,
                    AssignedAt = dtc.AssignedAt
                })
                .ToListAsync();

            return Ok(drivers);
        }

        // POST: api/Driver - CREAR conductor y VINCULAR a transportadora
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDriverDto dto)
        {
            // Validar que la transportadora existe
            var company = await _context.TransportCompanies.FindAsync(dto.TransportCompanyId);
            if (company == null)
                return BadRequest(new { message = "Transportadora no existe" });

            // Verificar si el conductor ya existe por cédula
            var existingDriver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.IdentificationNumber == dto.IdentificationNumber);

            int driverId;

            if (existingDriver != null)
            {
                driverId = existingDriver.Id;

                // Verificar si ya está vinculado a esta transportadora
                var existingLink = await _context.DriverTransportCompanies
                    .FirstOrDefaultAsync(dtc => dtc.DriverId == driverId && dtc.TransportCompanyId == dto.TransportCompanyId);

                if (existingLink != null)
                    return Conflict(new { message = "Este conductor ya está vinculado a esta transportadora" });
            }
            else
            {
                // Crear nuevo conductor
                var driver = new Driver
                {
                    IdentificationNumber = dto.IdentificationNumber,
                    FullName = dto.FullName.ToUpper(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Drivers.Add(driver);
                await _context.SaveChangesAsync();
                driverId = driver.Id;
            }

            // Crear el vínculo con la transportadora
            var driverCompany = new DriverTransportCompany
            {
                DriverId = driverId,
                TransportCompanyId = dto.TransportCompanyId,
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.DriverTransportCompanies.Add(driverCompany);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = existingDriver != null ? "Conductor vinculado exitosamente" : "Conductor creado y vinculado exitosamente",
                driverId = driverId,
                identificationNumber = dto.IdentificationNumber,
                fullName = dto.FullName.ToUpper(),
                transportCompanyId = dto.TransportCompanyId
            });
        }
    }

    public class CreateDriverDto
    {
        public string IdentificationNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int TransportCompanyId { get; set; }
    }
}