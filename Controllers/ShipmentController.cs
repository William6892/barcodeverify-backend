using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BarcodeShippingSystem.Data;
using BarcodeShippingSystem.Models;
using BarcodeShippingSystem.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BarcodeShippingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ShipmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ShipmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartShipment([FromBody] StartShipmentDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var shipment = await _context.Shipments
                .Include(s => s.TransportCompany)
                .FirstOrDefaultAsync(s => s.ShipmentNumber == dto.ShipmentNumber && s.Status == "Pending");

            if (shipment == null)
                return NotFound(new { message = "Envío no encontrado o ya está en progreso" });

            shipment.Status = "InProgress";
            shipment.UpdatedAt = DateTime.UtcNow;

            var scanOperation = new ScanOperation
            {
                ShipmentId = shipment.Id,
                UserId = userId,
                StartTime = DateTime.UtcNow,
                Status = "Active"
            };

            _context.ScanOperations.Add(scanOperation);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Escaneo iniciado",
                shipmentId = shipment.Id,
                shipmentNumber = shipment.ShipmentNumber,
                transportCompany = shipment.TransportCompany?.Name,
                scanOperationId = scanOperation.Id
            });
        }

        [HttpPost("scan")]
        public async Task<IActionResult> ScanProduct([FromBody] ScanProductDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var shipment = await _context.Shipments
                .Include(s => s.Products)
                .FirstOrDefaultAsync(s => s.Id == dto.ShipmentId && s.Status == "InProgress");

            if (shipment == null)
                return NotFound(new { message = "Envío no encontrado o no está en progreso" });

            // ✅ VALIDACIÓN 1: Verificar si ya existe un producto con el mismo barcode en este envío
            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Barcode == dto.Barcode && p.ShipmentId == dto.ShipmentId);

            if (existingProduct != null)
            {
                // ✅ VALIDACIÓN 2: Si el producto requiere número de serie, verificar que no se repita
                if (!string.IsNullOrEmpty(dto.SerialNumber))
                {
                    var existingWithSameSerial = await _context.Products
                        .FirstOrDefaultAsync(p => p.SerialNumber == dto.SerialNumber &&
                                                p.ShipmentId == dto.ShipmentId);

                    if (existingWithSameSerial != null)
                    {
                        return Conflict(new
                        {
                            message = "Ya existe un producto con este número de serie en el envío",
                            barcode = dto.Barcode,
                            serialNumber = dto.SerialNumber,
                            existingProduct = new
                            {
                                existingWithSameSerial.Id,
                                existingWithSameSerial.Name,
                                existingWithSameSerial.Quantity,
                                existingWithSameSerial.ScannedAt
                            }
                        });
                    }

                    // Si es el mismo barcode pero diferente serie, crear nuevo producto
                    var newProduct = new Product
                    {
                        Barcode = dto.Barcode,
                        Name = dto.Name ?? $"Producto {dto.Barcode}",
                        Description = dto.Description,
                        SKU = dto.SKU ?? dto.Barcode,
                        Quantity = dto.Quantity,
                        Category = dto.Category ?? "Electrónica",
                        Brand = "Samsung",
                        Model = dto.Model,
                        SerialNumber = dto.SerialNumber, // Diferente serie
                        ShipmentId = dto.ShipmentId,
                        ScannedAt = DateTime.UtcNow,
                        ScannedByUserId = userId
                    };

                    _context.Products.Add(newProduct);
                }
                else
                {
                    // Sin número de serie, incrementar cantidad
                    existingProduct.Quantity += dto.Quantity;
                    existingProduct.ScannedAt = DateTime.UtcNow;
                }
            }
            else
            {
                // ✅ VALIDACIÓN 3: Para productos nuevos con número de serie, verificar que no exista
                if (!string.IsNullOrEmpty(dto.SerialNumber))
                {
                    var existingSerial = await _context.Products
                        .FirstOrDefaultAsync(p => p.SerialNumber == dto.SerialNumber &&
                                                p.ShipmentId == dto.ShipmentId);

                    if (existingSerial != null)
                    {
                        return Conflict(new
                        {
                            message = "Ya existe un producto con este número de serie en el envío",
                            serialNumber = dto.SerialNumber,
                            existingProduct = new
                            {
                                existingSerial.Id,
                                existingSerial.Barcode,
                                existingSerial.Name,
                                existingSerial.Quantity
                            }
                        });
                    }
                }

                // Crear nuevo producto
                var product = new Product
                {
                    Barcode = dto.Barcode,
                    Name = dto.Name ?? $"Producto {dto.Barcode}",
                    Description = dto.Description,
                    SKU = dto.SKU ?? dto.Barcode,
                    Quantity = dto.Quantity,
                    Category = dto.Category ?? "Electrónica",
                    Brand = "Samsung",
                    Model = dto.Model,
                    SerialNumber = dto.SerialNumber,
                    ShipmentId = dto.ShipmentId,
                    ScannedAt = DateTime.UtcNow,
                    ScannedByUserId = userId
                };

                _context.Products.Add(product);
            }

            // Actualizar operación de escaneo
            var scanOperation = await _context.ScanOperations
                .FirstOrDefaultAsync(so => so.ShipmentId == dto.ShipmentId && so.UserId == userId && so.Status == "Active");

            if (scanOperation != null)
            {
                scanOperation.ProductCount = await _context.Products
                    .Where(p => p.ShipmentId == dto.ShipmentId)
                    .SumAsync(p => p.Quantity);
            }

            await _context.SaveChangesAsync();

            // Obtener conteo actualizado
            var totalAfterScan = await _context.Products
                .Where(p => p.ShipmentId == dto.ShipmentId)
                .SumAsync(p => p.Quantity);

            return Ok(new
            {
                message = "Producto escaneado exitosamente",
                shipmentId = dto.ShipmentId,
                barcode = dto.Barcode,
                serialNumber = dto.SerialNumber,
                quantity = dto.Quantity,
                productsCount = totalAfterScan,
                timestamp = DateTime.UtcNow,
                action = existingProduct != null ? "Cantidad incrementada" : "Nuevo producto añadido"
            });
        }

        [HttpPost("complete/{shipmentId}")]
        public async Task<IActionResult> CompleteShipment(int shipmentId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var shipment = await _context.Shipments
                .Include(s => s.Products)
                .Include(s => s.TransportCompany)
                .FirstOrDefaultAsync(s => s.Id == shipmentId && s.Status == "InProgress");

            if (shipment == null)
                return NotFound(new { message = "Envío no encontrado" });

            shipment.Status = "Completed";
            shipment.ActualDeparture = DateTime.UtcNow;
            shipment.UpdatedAt = DateTime.UtcNow;

            // Finalizar operación de escaneo
            var scanOperation = await _context.ScanOperations
                .FirstOrDefaultAsync(so => so.ShipmentId == shipmentId && so.UserId == userId && so.Status == "Active");

            if (scanOperation != null)
            {
                scanOperation.EndTime = DateTime.UtcNow;
                scanOperation.Status = "Completed";
                scanOperation.ProductCount = shipment.Products?.Sum(p => p.Quantity) ?? 0;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Envío completado exitosamente",
                shipmentNumber = shipment.ShipmentNumber,
                totalProducts = shipment.Products?.Sum(p => p.Quantity) ?? 0,
                transportCompany = shipment.TransportCompany?.Name,
                driver = shipment.TransportCompany?.DriverName,
                licensePlate = shipment.TransportCompany?.LicensePlate,
                departureTime = shipment.ActualDeparture
            });
        }

        // Endpoint para envíos activos (Pending e InProgress)
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveShipments()
        {
            try
            {
                var shipments = await _context.Shipments
                    .Include(s => s.TransportCompany)
                    .Include(s => s.Products)
                    .Where(s => s.Status == "InProgress" || s.Status == "Pending")
                    .Select(s => new
                    {
                        s.Id,
                        s.ShipmentNumber,
                        s.Status,
                        TransportCompany = s.TransportCompany != null ? new
                        {
                            s.TransportCompany.Name,
                            s.TransportCompany.DriverName,
                            s.TransportCompany.LicensePlate,
                            s.TransportCompany.Phone
                        } : null,
                        ProductCount = s.Products.Sum(p => p.Quantity),
                        s.CreatedAt,
                        s.EstimatedDeparture
                    })
                    .ToListAsync();

                return Ok(shipments);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error al obtener envíos activos" });
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateShipment([FromBody] CreateShipmentDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Verificar que la transportadora existe
            var transportCompany = await _context.TransportCompanies
                .FirstOrDefaultAsync(tc => tc.Id == dto.TransportCompanyId);

            if (transportCompany == null)
                return NotFound(new { message = "Transportadora no encontrada" });

            var shipment = new Shipment
            {
                ShipmentNumber = dto.ShipmentNumber ?? GenerateShipmentNumber(),
                TransportCompanyId = dto.TransportCompanyId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId,
                EstimatedDeparture = dto.EstimatedDeparture.HasValue
                    ? DateTime.SpecifyKind(dto.EstimatedDeparture.Value, DateTimeKind.Utc)
                    : (DateTime?)null
            };

            _context.Shipments.Add(shipment);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Envío creado exitosamente",
                shipmentId = shipment.Id,
                shipmentNumber = shipment.ShipmentNumber,
                transportCompany = transportCompany.Name,
                driver = transportCompany.DriverName,
                licensePlate = transportCompany.LicensePlate
            });
        }

        // Nuevo endpoint para cambiar estado (usuarios normales)
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateShipmentStatus(int id, [FromBody] UpdateShipmentStatusDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var shipment = await _context.Shipments.FindAsync(id);

            if (shipment == null)
                return NotFound(new { message = "Envío no encontrado" });

            // Validar que el usuario es el creador o es admin
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (shipment.CreatedByUserId != userId && userRole != "Admin")
                return Forbid();

            // Validar transiciones de estado
            var allowedTransitions = new Dictionary<string, string[]>
            {
                ["Pending"] = new[] { "InProgress", "Cancelled" },
                ["InProgress"] = new[] { "Completed", "Cancelled" },
                ["Completed"] = new string[] { }, // No se puede cambiar
                ["Cancelled"] = new string[] { }  // No se puede cambiar
            };

            if (!allowedTransitions.ContainsKey(shipment.Status) ||
                !allowedTransitions[shipment.Status].Contains(dto.Status))
            {
                return BadRequest(new
                {
                    message = $"No se puede cambiar de '{shipment.Status}' a '{dto.Status}'",
                    allowedTransitions = allowedTransitions[shipment.Status]
                });
            }

            shipment.Status = dto.Status;
            shipment.UpdatedAt = DateTime.UtcNow;

            // Si se completa, registrar hora de salida
            if (dto.Status == "Completed" && !shipment.ActualDeparture.HasValue)
            {
                shipment.ActualDeparture = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Estado actualizado a '{dto.Status}'",
                shipmentId = shipment.Id,
                previousStatus = shipment.Status,
                newStatus = dto.Status,
                updatedAt = shipment.UpdatedAt
            });
        }

        [HttpPatch("{id}/cancel")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CancelShipment(int id)
        {
            try
            {
                var shipment = await _context.Shipments
                    .Include(s => s.Products)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (shipment == null)
                    return NotFound(new { message = "Envío no encontrado" });

                // Solo se puede cancelar si está en Pending o InProgress
                if (shipment.Status != "Pending" && shipment.Status != "InProgress")
                {
                    return BadRequest(new
                    {
                        message = $"No se puede cancelar un envío en estado '{shipment.Status}'"
                    });
                }

                shipment.Status = "Cancelled";
                shipment.UpdatedAt = DateTime.UtcNow;

                // Si hay una operación de escaneo activa, finalizarla
                var activeScan = await _context.ScanOperations
                    .FirstOrDefaultAsync(so => so.ShipmentId == id && so.Status == "Active");

                if (activeScan != null)
                {
                    activeScan.EndTime = DateTime.UtcNow;
                    activeScan.Status = "Cancelled";
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Envío cancelado exitosamente",
                    shipmentNumber = shipment.ShipmentNumber,
                    previousStatus = shipment.Status,
                    cancelledAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al cancelar envío", error = ex.Message });
            }
        }

        // NUEVO: Obtener TODOS los envíos
        [HttpGet("all")]
        public async Task<IActionResult> GetAllShipments()
        {
            try
            {
                var shipments = await _context.Shipments
                    .Include(s => s.TransportCompany)
                    .Include(s => s.Products)
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new
                    {
                        s.Id,
                        s.ShipmentNumber,
                        s.Status,
                        TransportCompany = s.TransportCompany != null ? new
                        {
                            s.TransportCompany.Name,
                            s.TransportCompany.DriverName,
                            s.TransportCompany.LicensePlate,
                            s.TransportCompany.Phone
                        } : null,
                        ProductCount = s.Products.Sum(p => p.Quantity),
                        s.CreatedAt,
                        s.EstimatedDeparture,
                        s.ActualDeparture,
                        s.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(shipments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener envíos", error = ex.Message });
            }
        }

        // NUEVO: Obtener envíos completados
        [HttpGet("completed")]
        public async Task<IActionResult> GetCompletedShipments()
        {
            try
            {
                var shipments = await _context.Shipments
                    .Include(s => s.TransportCompany)
                    .Include(s => s.Products)
                    .Where(s => s.Status == "Completed")
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new
                    {
                        s.Id,
                        s.ShipmentNumber,
                        s.Status,
                        TransportCompany = s.TransportCompany != null ? new
                        {
                            s.TransportCompany.Name,
                            s.TransportCompany.DriverName,
                            s.TransportCompany.LicensePlate,
                            s.TransportCompany.Phone
                        } : null,
                        ProductCount = s.Products.Sum(p => p.Quantity),
                        s.CreatedAt,
                        s.EstimatedDeparture,
                        s.ActualDeparture,
                        CompletedAt = s.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(shipments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener envíos completados", error = ex.Message });
            }
        }

        // NUEVO: Obtener envíos cancelados
        [HttpGet("cancelled")]
        public async Task<IActionResult> GetCancelledShipments()
        {
            try
            {
                var shipments = await _context.Shipments
                    .Include(s => s.TransportCompany)
                    .Include(s => s.Products)
                    .Where(s => s.Status == "Cancelled")
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new
                    {
                        s.Id,
                        s.ShipmentNumber,
                        s.Status,
                        TransportCompany = s.TransportCompany != null ? new
                        {
                            s.TransportCompany.Name,
                            s.TransportCompany.DriverName,
                            s.TransportCompany.LicensePlate,
                            s.TransportCompany.Phone
                        } : null,
                        ProductCount = s.Products.Sum(p => p.Quantity),
                        s.CreatedAt,
                        s.EstimatedDeparture,
                        CancelledAt = s.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(shipments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener envíos cancelados", error = ex.Message });
            }
        }

        // NUEVO: Obtener envío por ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetShipmentById(int id)
        {
            try
            {
                var shipment = await _context.Shipments
                    .Include(s => s.TransportCompany)
                    .Include(s => s.Products)
                    .Where(s => s.Id == id)
                    .Select(s => new
                    {
                        s.Id,
                        s.ShipmentNumber,
                        s.Status,
                        TransportCompany = s.TransportCompany != null ? new
                        {
                            s.TransportCompany.Id,
                            s.TransportCompany.Name,
                            s.TransportCompany.DriverName,
                            s.TransportCompany.LicensePlate,
                            s.TransportCompany.Phone
                        } : null,
                        Products = s.Products.Select(p => new
                        {
                            p.Id,
                            p.Barcode,
                            p.Name,
                            p.Description,
                            p.Quantity,
                            p.Category,
                            p.Brand,
                            p.Model,
                            p.SerialNumber,
                            p.ScannedAt
                        }).ToList(),
                        ProductCount = s.Products.Sum(p => p.Quantity),
                        s.CreatedAt,
                        s.EstimatedDeparture,
                        s.ActualDeparture,
                        s.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (shipment == null)
                    return NotFound(new { message = "Envío no encontrado" });

                return Ok(shipment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener envío", error = ex.Message });
            }
        }

        // NUEVO: Obtener envío por número
        [HttpGet("number/{shipmentNumber}")]
        public async Task<IActionResult> GetShipmentByNumber(string shipmentNumber)
        {
            try
            {
                var shipment = await _context.Shipments
                    .Include(s => s.TransportCompany)
                    .Include(s => s.Products)
                    .Where(s => s.ShipmentNumber == shipmentNumber)
                    .Select(s => new
                    {
                        s.Id,
                        s.ShipmentNumber,
                        s.Status,
                        TransportCompany = s.TransportCompany != null ? new
                        {
                            s.TransportCompany.Name,
                            s.TransportCompany.DriverName,
                            s.TransportCompany.LicensePlate,
                            s.TransportCompany.Phone
                        } : null,
                        Products = s.Products.Select(p => new
                        {
                            p.Barcode,
                            p.Name,
                            p.Quantity,
                            p.Category,
                            p.ScannedAt
                        }).ToList(),
                        ProductCount = s.Products.Sum(p => p.Quantity),
                        s.CreatedAt,
                        s.EstimatedDeparture,
                        s.ActualDeparture
                    })
                    .FirstOrDefaultAsync();

                if (shipment == null)
                    return NotFound(new { message = "Envío no encontrado" });

                return Ok(shipment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener envío", error = ex.Message });
            }
        }

        // NUEVO: Buscar envíos con filtros
        [HttpGet("search")]
        public async Task<IActionResult> SearchShipments(
            [FromQuery] string? status,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] string? shipmentNumber)
        {
            try
            {
                var query = _context.Shipments
                    .Include(s => s.TransportCompany)
                    .Include(s => s.Products)
                    .AsQueryable();

                // Filtrar por estado
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(s => s.Status == status);
                }

                // Filtrar por número de envío
                if (!string.IsNullOrEmpty(shipmentNumber))
                {
                    query = query.Where(s => s.ShipmentNumber.Contains(shipmentNumber));
                }

                // Filtrar por fecha de creación (desde)
                if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var fromDate))
                {
                    query = query.Where(s => s.CreatedAt >= fromDate);
                }

                // Filtrar por fecha de creación (hasta)
                if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var toDate))
                {
                    query = query.Where(s => s.CreatedAt <= toDate.AddDays(1).AddSeconds(-1));
                }

                var shipments = await query
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new
                    {
                        s.Id,
                        s.ShipmentNumber,
                        s.Status,
                        TransportCompany = s.TransportCompany != null ? new
                        {
                            s.TransportCompany.Name,
                            s.TransportCompany.DriverName,
                            s.TransportCompany.LicensePlate
                        } : null,
                        ProductCount = s.Products.Sum(p => p.Quantity),
                        s.CreatedAt,
                        s.EstimatedDeparture,
                        s.ActualDeparture
                    })
                    .ToListAsync();

                return Ok(shipments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al buscar envíos", error = ex.Message });
            }
        }

        // NUEVO: Obtener estadísticas
        [HttpGet("stats")]
        public async Task<IActionResult> GetShipmentStats()
        {
            try
            {
                var total = await _context.Shipments.CountAsync();
                var pending = await _context.Shipments.CountAsync(s => s.Status == "Pending");
                var inProgress = await _context.Shipments.CountAsync(s => s.Status == "InProgress");
                var completed = await _context.Shipments.CountAsync(s => s.Status == "Completed");
                var cancelled = await _context.Shipments.CountAsync(s => s.Status == "Cancelled");

                var totalProducts = await _context.Products.SumAsync(p => p.Quantity);
                var averageProducts = total > 0 ? totalProducts / total : 0;

                return Ok(new
                {
                    Total = total,
                    Pending = pending,
                    InProgress = inProgress,
                    Completed = completed,
                    Cancelled = cancelled,
                    TotalProducts = totalProducts,
                    AverageProductsPerShipment = averageProducts,
                    Today = await _context.Shipments.CountAsync(s => s.CreatedAt.Date == DateTime.UtcNow.Date),
                    ThisWeek = await _context.Shipments.CountAsync(s =>
                        s.CreatedAt >= DateTime.UtcNow.AddDays(-7)),
                    ThisMonth = await _context.Shipments.CountAsync(s =>
                        s.CreatedAt.Month == DateTime.UtcNow.Month &&
                        s.CreatedAt.Year == DateTime.UtcNow.Year)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener estadísticas", error = ex.Message });
            }
        }

        private string GenerateShipmentNumber()
        {
            return $"SH{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }
    }
}