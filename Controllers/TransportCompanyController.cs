using BarcodeShippingSystem.Data;
using BarcodeShippingSystem.DTOs;
using BarcodeShippingSystem.Models;
using BarcodeShippingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BarcodeShippingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requiere autenticación para todos los endpoints
    public class TransportCompanyController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ITransportCompanyService _transportCompanyService;

        public TransportCompanyController(
            ApplicationDbContext context,
            ITransportCompanyService transportCompanyService)
        {
            _context = context;
            _transportCompanyService = transportCompanyService;
        }

        // GET: api/TransportCompany
        [HttpGet]
        [AllowAnonymous] // Permitir acceso público a la lista
        public async Task<IActionResult> GetTransportCompanies([FromQuery] bool activeOnly = true)
        {
            try
            {
                var companies = await _transportCompanyService.GetAllAsync(activeOnly);
                return Ok(companies);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // GET: api/TransportCompany/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTransportCompany(int id)
        {
            try
            {
                var company = await _transportCompanyService.GetByIdAsync(id);

                if (company == null)
                    return NotFound(new { message = "Transportadora no encontrada" });

                return Ok(company);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // POST: api/TransportCompany/user
        [HttpPost("user")]
        public async Task<IActionResult> CreateTransportCompanyForUser([FromBody] CreateTransportCompanyDto dto)
        {
            try
            {
                // Obtener el usuario actual del token JWT
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                var userName = User.FindFirst(ClaimTypes.Name)?.Value;

                // Validar que el usuario está autenticado
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Usuario no autenticado" });
                }

                // Verificar si ya existe una transportadora con la misma placa
                var existingCompany = await _context.TransportCompanies
                    .FirstOrDefaultAsync(tc => tc.LicensePlate.ToLower() == dto.LicensePlate.ToLower());

                if (existingCompany != null)
                {
                    return Conflict(new
                    {
                        message = "Ya existe una transportadora con esta placa",
                        existingCompany = new
                        {
                            existingCompany.Id,
                            existingCompany.Name,
                            existingCompany.LicensePlate,
                            existingCompany.IsActive
                        }
                    });
                }

                // Crear la transportadora
                var company = await _transportCompanyService.CreateAsync(dto);

                // Opcional: Registrar quién creó esta transportadora
                // Podrías agregar campos CreatedByUserId y CreatedByUserEmail al modelo
                // o crear una tabla de logs si necesitas tracking

                return CreatedAtAction(nameof(GetTransportCompany), new { id = company.Id }, new
                {
                    message = "Transportadora creada exitosamente",
                    success = true,
                    data = new
                    {
                        company.Id,
                        company.Name,
                        company.Phone,
                        company.DriverName,
                        company.LicensePlate,
                        company.IsActive,
                        company.CreatedAt,
                        createdBy = new
                        {
                            userId,
                            userName,
                            userEmail
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error interno del servidor",
                    error = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }

        // POST: api/TransportCompany
        [HttpPost]
        [Authorize(Roles = "Admin")] // SOLO Admin puede usar este endpoint
        public async Task<IActionResult> CreateTransportCompanyAdmin([FromBody] CreateTransportCompanyDto dto)
        {
            try
            {
                // Verificar si ya existe una transportadora con la misma placa
                var existingCompany = await _context.TransportCompanies
                    .FirstOrDefaultAsync(tc => tc.LicensePlate.ToLower() == dto.LicensePlate.ToLower());

                if (existingCompany != null)
                    return Conflict(new { message = "Ya existe una transportadora con esta placa" });

                var company = await _transportCompanyService.CreateAsync(dto);

                return CreatedAtAction(nameof(GetTransportCompany), new { id = company.Id }, new
                {
                    message = "Transportadora creada exitosamente (Admin)",
                    companyId = company.Id,
                    company.Name,
                    company.Phone,
                    company.DriverName,
                    company.LicensePlate
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // PUT: api/TransportCompany/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] // SOLO Admin puede actualizar
        public async Task<IActionResult> UpdateTransportCompany(int id, [FromBody] UpdateTransportCompanyDto dto)
        {
            try
            {
                // Si se actualiza la placa, verificar que no exista otra con la misma
                if (!string.IsNullOrEmpty(dto.LicensePlate))
                {
                    var existingWithSamePlate = await _context.TransportCompanies
                        .FirstOrDefaultAsync(tc => tc.LicensePlate.ToLower() == dto.LicensePlate.ToLower() && tc.Id != id);

                    if (existingWithSamePlate != null)
                        return Conflict(new { message = "Ya existe otra transportadora con esta placa" });
                }

                var result = await _transportCompanyService.UpdateAsync(id, dto);

                if (!result)
                    return NotFound(new { message = "Transportadora no encontrada" });

                return Ok(new { message = "Transportadora actualizada exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // DELETE: api/TransportCompany/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // SOLO Admin puede eliminar
        public async Task<IActionResult> DeleteTransportCompany(int id)
        {
            try
            {
                var result = await _transportCompanyService.DeleteAsync(id);

                if (!result)
                    return BadRequest(new { message = "No se puede eliminar, tiene envíos asociados o no existe" });

                return Ok(new { message = "Transportadora eliminada exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // PATCH: api/TransportCompany/5/toggle-status
        [HttpPatch("{id}/toggle-status")]
        [Authorize(Roles = "Admin")] // SOLO Admin puede cambiar estado
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var result = await _transportCompanyService.ToggleStatusAsync(id);

                if (!result)
                    return NotFound(new { message = "Transportadora no encontrada" });

                return Ok(new { message = "Estado cambiado exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // GET: api/TransportCompany/search?plate=ABC123
        [HttpGet("search")]
        public async Task<IActionResult> SearchByLicensePlate([FromQuery] string plate)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(plate))
                    return BadRequest(new { message = "Debe proporcionar una placa para buscar" });

                var company = await _transportCompanyService.SearchByLicensePlateAsync(plate);

                if (company == null)
                    return NotFound(new { message = "No se encontró transportadora con esa placa" });

                return Ok(company);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }
    }
}