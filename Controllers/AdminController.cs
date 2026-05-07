// Controllers/AdminController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BarcodeShippingSystem.Data;
using BarcodeShippingSystem.Models;
using BarcodeShippingSystem.DTOs;
using BarcodeShippingSystem.Services;

namespace BarcodeShippingSystem.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAdminService _adminService;

        public AdminController(ApplicationDbContext context, IAdminService adminService)
        {
            _context = context;
            _adminService = adminService;
        }

        [HttpGet("dashboard/stats")]
        public async Task<IActionResult> GetDashboardStats([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            var result = await _adminService.GetDashboardStatsAsync(startDate, endDate);
            return Ok(result);
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _adminService.GetAllUsersAsync();
            return Ok(result);
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                return BadRequest(new { message = "El nombre de usuario ya existe" });

            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { message = "El email ya está registrado" });

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = passwordHash,
                Role = dto.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Usuario creado exitosamente",
                user = new { user.Id, user.Username, user.Email, user.Role }
            });
        }

        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateUserRoleDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "Usuario no encontrado" });

            user.Role = dto.Role;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Rol actualizado a {dto.Role}" });
        }

        [HttpPut("users/{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "Usuario no encontrado" });

            user.IsActive = dto.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Usuario {(dto.IsActive ? "activado" : "desactivado")}" });
        }

        [HttpGet("transport-companies")]
        public async Task<IActionResult> GetAllTransportCompanies()
        {
            var result = await _adminService.GetAllTransportCompaniesAsync();
            return Ok(result);
        }

        [HttpPost("transport-companies")]
        public async Task<IActionResult> CreateTransportCompany([FromBody] CreateTransportCompanyDto dto)
        {
            if (await _context.TransportCompanies.AnyAsync(tc => tc.Name.ToLower() == dto.Name.ToLower()))
                return BadRequest(new { message = "Ya existe una transportadora con este nombre" });

            var company = new TransportCompany
            {
                Name = dto.Name,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.TransportCompanies.Add(company);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Transportadora creada exitosamente",
                company = new { company.Id, company.Name }
            });
        }

        [HttpGet("products/search")]
        public async Task<IActionResult> SearchProducts(
            [FromQuery] string? barcode = null,
            [FromQuery] string? name = null,
            [FromQuery] string? category = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _adminService.SearchProductsAsync(barcode, name, category, startDate, endDate, page, pageSize);
            return Ok(result);
        }

        [HttpGet("reports/shipments")]
        public async Task<IActionResult> GenerateShipmentReport(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var result = await _adminService.GenerateShipmentReportAsync(startDate, endDate);
            return Ok(result);
        }

        [HttpGet("stats/quick")]
        public async Task<IActionResult> GetQuickStats()
        {
            var result = await _adminService.GetQuickStatsAsync();
            return Ok(result);
        }
    }
}