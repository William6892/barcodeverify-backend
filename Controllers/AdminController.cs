using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BarcodeShippingSystem.Data;
using BarcodeShippingSystem.Models;
using BarcodeShippingSystem.DTOs;
using System.Security.Cryptography;
using System.Text;

namespace BarcodeShippingSystem.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================
        // DASHBOARD & ESTADÍSTICAS
        // ============================

        [HttpGet("dashboard/stats")]
        public async Task<IActionResult> GetDashboardStats([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // Si no se especifican fechas, usar el mes actual
                var start = startDate ?? DateTime.UtcNow.Date.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

                // Estadísticas generales
                var totalShipments = await _context.Shipments
                    .Where(s => s.CreatedAt >= start && s.CreatedAt <= end)
                    .CountAsync();

                // Total productos escaneados
                var productsQuery = _context.Products
                    .Where(p => p.ScannedAt >= start && p.ScannedAt <= end);

                var totalProductsScanned = await productsQuery.AnyAsync()
                    ? await productsQuery.SumAsync(p => p.Quantity)
                    : 0;

                var totalUsersActive = await _context.Users
                    .Where(u => u.IsActive && u.LastLogin >= start)
                    .CountAsync();

                // Transportadoras con más envíos (TOP 5)
                var topTransportCompanies = await _context.Shipments
                    .Include(s => s.TransportCompany)
                    .Where(s => s.CreatedAt >= start && s.CreatedAt <= end)
                    .GroupBy(s => s.TransportCompanyId)
                    .Select(g => new
                    {
                        CompanyId = g.Key,
                        CompanyName = g.First().TransportCompany != null ? g.First().TransportCompany.Name : "Desconocida",
                        DriverName = g.First().TransportCompany != null ? g.First().TransportCompany.DriverName : "Desconocido",
                        ShipmentCount = g.Count(),
                        ProductCount = g.SelectMany(s => s.Products).Sum(p => p.Quantity)
                    })
                    .OrderByDescending(x => x.ShipmentCount)
                    .Take(5)
                    .ToListAsync();

                // Usuarios con más escaneos (TOP 5)
                var topUsers = await _context.ScanOperations
                    .Include(so => so.User)
                    .Where(so => so.StartTime >= start && so.StartTime <= end)
                    .GroupBy(so => so.UserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        Username = g.First().User != null ? g.First().User.Username : "Desconocido",
                        Email = g.First().User != null ? g.First().User.Email : "",
                        ScanCount = g.Count(),
                        TotalProductsScanned = g.Sum(so => so.ProductCount), 
                        LastScan = g.Max(so => so.StartTime)
                    })
                    .OrderByDescending(x => x.TotalProductsScanned)
                    .Take(5)
                    .ToListAsync();

                // Productos más escaneados (TOP 10)
                var topProducts = await _context.Products
                    .Where(p => p.ScannedAt >= start && p.ScannedAt <= end)
                    .GroupBy(p => p.Barcode)
                    .Select(g => new
                    {
                        Barcode = g.Key,
                        Name = g.First().Name,
                        Category = g.First().Category,
                        Brand = g.First().Brand,
                        TotalQuantity = g.Sum(p => p.Quantity),
                        ShipmentCount = g.Select(p => p.ShipmentId).Distinct().Count()
                    })
                    .OrderByDescending(x => x.TotalQuantity)
                    .Take(10)
                    .ToListAsync();

                // Envíos por estado
                var shipmentsByStatus = await _context.Shipments
                    .Where(s => s.CreatedAt >= start && s.CreatedAt <= end)
                    .GroupBy(s => s.Status)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count(),
                        Percentage = totalShipments > 0 ? Math.Round((double)g.Count() / totalShipments * 100, 1) : 0
                    })
                    .ToListAsync();

                // Actividad diaria (últimos 7 días)
                var dailyActivity = await _context.ScanOperations
                    .Where(so => so.StartTime >= DateTime.UtcNow.Date.AddDays(-7))
                    .GroupBy(so => so.StartTime.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        ScanOperations = g.Count(),
                        ProductsScanned = g.Sum(so => so.ProductCount) // CORREGIDO
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                return Ok(new
                {
                    Period = new { Start = start, End = end },
                    Summary = new
                    {
                        TotalShipments = totalShipments,
                        TotalProductsScanned = totalProductsScanned,
                        TotalUsersActive = totalUsersActive,
                        AvgProductsPerShipment = totalShipments > 0 ? Math.Round((double)totalProductsScanned / totalShipments, 1) : 0
                    },
                    TopTransportCompanies = topTransportCompanies,
                    TopUsers = topUsers,
                    TopProducts = topProducts,
                    ShipmentsByStatus = shipmentsByStatus,
                    DailyActivity = dailyActivity
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener estadísticas", error = ex.Message });
            }
        }

        // ============================
        // GESTIÓN DE USUARIOS
        // ============================

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _context.Users
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Email,
                        u.Role,
                        u.IsActive,
                        u.CreatedAt,
                        u.LastLogin,
                        TotalScans = _context.ScanOperations.Count(so => so.UserId == u.Id),
                        TotalProductsScanned = _context.ScanOperations
                            .Where(so => so.UserId == u.Id)
                            .Sum(so => so.ProductCount) // CORREGIDO
                    })
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener usuarios", error = ex.Message });
            }
        }

            [HttpPost("users")]
            public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
            {
                try
                {
                    // Verificar si el usuario ya existe
                    if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                        return BadRequest(new { message = "El nombre de usuario ya existe" });

                    if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                        return BadRequest(new { message = "El email ya está registrado" });

                    // Crear hash de la contraseña
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
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "Error al crear usuario", error = ex.Message });
                }
            }

        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateUserRoleDto dto)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound(new { message = "Usuario no encontrado" });

                user.Role = dto.Role;
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Rol actualizado a {dto.Role}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar rol", error = ex.Message });
            }
        }

        [HttpPut("users/{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusDto dto)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound(new { message = "Usuario no encontrado" });

                user.IsActive = dto.IsActive;
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Usuario {(dto.IsActive ? "activado" : "desactivado")}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar estado", error = ex.Message });
            }
        }

        // ============================
        // GESTIÓN DE TRANSPORTADORAS
        // ============================

        [HttpGet("transport-companies")]
        public async Task<IActionResult> GetAllTransportCompanies()
        {
            try
            {
                var companies = await _context.TransportCompanies
                    .Select(tc => new
                    {
                        tc.Id,
                        tc.Name,
                        tc.DriverName,
                        tc.LicensePlate,
                        tc.Phone,
                        tc.IsActive,
                        tc.CreatedAt,
                        TotalShipments = _context.Shipments.Count(s => s.TransportCompanyId == tc.Id),
                        TotalProducts = _context.Shipments
                            .Where(s => s.TransportCompanyId == tc.Id)
                            .SelectMany(s => s.Products)
                            .Sum(p => p.Quantity)
                    })
                    .OrderByDescending(tc => tc.CreatedAt)
                    .ToListAsync();

                return Ok(companies);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener transportadoras", error = ex.Message });
            }
        }

        [HttpPost("transport-companies")]
        public async Task<IActionResult> CreateTransportCompany([FromBody] CreateTransportCompanyDto dto)
        {
            try
            {
                if (await _context.TransportCompanies.AnyAsync(tc => tc.LicensePlate == dto.LicensePlate))
                    return BadRequest(new { message = "Ya existe una transportadora con esta placa" });

                var company = new TransportCompany
                {
                    Name = dto.Name,
                    DriverName = dto.DriverName,
                    LicensePlate = dto.LicensePlate,
                    Phone = dto.Phone,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TransportCompanies.Add(company);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Transportadora creada exitosamente",
                    company = new { company.Id, company.Name, company.DriverName, company.LicensePlate }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al crear transportadora", error = ex.Message });
            }
        }

        // ============================
        // BÚSQUEDA DE PRODUCTOS
        // ============================

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
            try
            {
                var query = _context.Products
                    .Include(p => p.Shipment)
                        .ThenInclude(s => s.TransportCompany)
                    .Include(p => p.ScannedByUser)
                    .AsQueryable();

                // Filtros
                if (!string.IsNullOrEmpty(barcode))
                    query = query.Where(p => p.Barcode.Contains(barcode));

                if (!string.IsNullOrEmpty(name))
                    query = query.Where(p => p.Name.Contains(name));

                if (!string.IsNullOrEmpty(category))
                    query = query.Where(p => p.Category == category);

                if (startDate.HasValue)
                    query = query.Where(p => p.ScannedAt >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(p => p.ScannedAt <= endDate.Value.AddDays(1).AddTicks(-1));

                // Paginación
                var totalCount = await query.CountAsync();
                var products = await query
                    .OrderByDescending(p => p.ScannedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.Id,
                        p.Barcode,
                        p.Name,
                        p.Description,
                        p.SKU,
                        p.Quantity,
                        p.Category,
                        p.Brand,
                        p.Model,
                        p.SerialNumber,
                        ScannedAt = p.ScannedAt,
                        ScannedBy = p.ScannedByUser != null ? new
                        {
                            p.ScannedByUser.Username,
                            p.ScannedByUser.Email
                        } : null,
                        Shipment = p.Shipment != null ? new
                        {
                            p.Shipment.Id,
                            p.Shipment.ShipmentNumber,
                            p.Shipment.Status,
                            TransportCompany = p.Shipment.TransportCompany != null ? new
                            {
                                p.Shipment.TransportCompany.Name,
                                p.Shipment.TransportCompany.DriverName
                            } : null
                        } : null
                    })
                    .ToListAsync();

                return Ok(new
                {
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    Products = products
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error en la búsqueda", error = ex.Message });
            }
        }

        // ============================
        // REPORTES
        // ============================

        [HttpGet("reports/shipments")]
        public async Task<IActionResult> GenerateShipmentReport(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.Date.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

                var shipments = await _context.Shipments
                    .Include(s => s.TransportCompany)
                    .Include(s => s.Products)
                    .Where(s => s.CreatedAt >= start && s.CreatedAt <= end)
                    .Select(s => new
                    {
                        s.Id,
                        s.ShipmentNumber,
                        s.Status,
                        s.CreatedAt,
                        s.ActualDeparture,
                        TransportCompany = s.TransportCompany != null ? new
                        {
                            s.TransportCompany.Name,
                            s.TransportCompany.DriverName,
                            s.TransportCompany.LicensePlate
                        } : null,
                        TotalProducts = s.Products.Sum(p => p.Quantity),
                        Products = s.Products.Select(p => new
                        {
                            p.Barcode,
                            p.Name,
                            p.Quantity,
                            p.Category,
                            p.Brand
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    ReportPeriod = new { Start = start, End = end },
                    TotalShipments = shipments.Count,
                    TotalProducts = shipments.Sum(s => s.TotalProducts),
                    Shipments = shipments
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al generar reporte", error = ex.Message });
            }
        }

        // ============================
        // ESTADÍSTICAS RÁPIDAS
        // ============================

        [HttpGet("stats/quick")]
        public async Task<IActionResult> GetQuickStats()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var tomorrow = today.AddDays(1);

                var shipmentsToday = await _context.Shipments
                    .Where(s => s.CreatedAt >= today && s.CreatedAt < tomorrow)
                    .CountAsync();

                var productsQuery = _context.Products
                    .Where(p => p.ScannedAt >= today && p.ScannedAt < tomorrow);

                var productsToday = await productsQuery.AnyAsync()
                    ? await productsQuery.SumAsync(p => p.Quantity)
                    : 0;

                var activeUsers = await _context.Users
                    .Where(u => u.IsActive)
                    .CountAsync();

                var pendingShipments = await _context.Shipments
                    .Where(s => s.Status == "Pending")
                    .CountAsync();

                var inProgressShipments = await _context.Shipments
                    .Where(s => s.Status == "InProgress")
                    .CountAsync();

                return Ok(new
                {
                    Today = new
                    {
                        Shipments = shipmentsToday,
                        ProductsScanned = productsToday
                    },
                    System = new
                    {
                        ActiveUsers = activeUsers,
                        PendingShipments = pendingShipments,
                        InProgressShipments = inProgressShipments
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener estadísticas rápidas", error = ex.Message });
            }
        }
    }
}