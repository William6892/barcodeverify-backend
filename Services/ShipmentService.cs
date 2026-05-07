// Services/ShipmentService.cs - Usando TUS DTOs
using Microsoft.EntityFrameworkCore;
using BarcodeShippingSystem.Data;
using BarcodeShippingSystem.Models;
using BarcodeShippingSystem.DTOs;

namespace BarcodeShippingSystem.Services
{
    public class ShipmentService : IShipmentService
    {
        private readonly ApplicationDbContext _context;

        public ShipmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ShipmentResponseDto> CreateShipmentAsync(CreateShipmentDto dto, int userId)
        {
            // Validar transportadora
            var transportCompany = await _context.TransportCompanies
                .FirstOrDefaultAsync(tc => tc.Id == dto.TransportCompanyId && tc.IsActive);

            if (transportCompany == null)
                throw new Exception("Transportadora no encontrada");

            // Validar conductor
            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.Id == dto.DriverId &&
                                          d.TransportCompanyId == dto.TransportCompanyId &&
                                          d.IsActive);
            if (driver == null)
                throw new Exception("Conductor no válido para esta transportadora");

            // Validar vehículo
            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == dto.VehicleId &&
                                          v.TransportCompanyId == dto.TransportCompanyId &&
                                          v.IsActive);
            if (vehicle == null)
                throw new Exception("Vehículo no válido para esta transportadora");

            // Verificar vehículo no tenga envíos activos
            var hasActiveShipment = await _context.Shipments
                .AnyAsync(s => s.VehicleId == dto.VehicleId &&
                               (s.Status == "Pending" || s.Status == "InProgress"));
            if (hasActiveShipment)
                throw new Exception("Este vehículo ya tiene un envío activo");

            // Crear envío
            var shipment = new Shipment
            {
                ShipmentNumber = dto.ShipmentNumber ?? GenerateShipmentNumber(),
                TransportCompanyId = dto.TransportCompanyId,
                DriverId = dto.DriverId,
                VehicleId = dto.VehicleId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId,
                EstimatedDeparture = dto.EstimatedDeparture?.ToUniversalTime(),
                Notes = dto.Notes
            };

            _context.Shipments.Add(shipment);
            await _context.SaveChangesAsync();

            // Retornar TU DTO
            return new ShipmentResponseDto
            {
                Id = shipment.Id,
                ShipmentNumber = shipment.ShipmentNumber,
                Status = shipment.Status,
                CreatedAt = shipment.CreatedAt,
                EstimatedDeparture = shipment.EstimatedDeparture,
                Notes = shipment.Notes,
                TransportCompany = new TransportCompanyDto
                {
                    Id = transportCompany.Id,
                    Name = transportCompany.Name,
                    IsActive = transportCompany.IsActive
                },
                Driver = new DriverInfoDto
                {
                    Id = driver.Id,
                    IdentificationNumber = driver.IdentificationNumber,
                    FullName = driver.FullName
                },
                Vehicle = new VehicleInfoDto
                {
                    Id = vehicle.Id,
                    PlateNumber = vehicle.PlateNumber,
                    TrailerPlate = vehicle.TrailerPlate,
                    VehicleType = vehicle.VehicleType
                }
            };
        }

        public async Task<ShipmentResponseDto> GetShipmentByIdAsync(int id)
        {
            var shipment = await _context.Shipments
                .Include(s => s.TransportCompany)
                .Include(s => s.Driver)
                .Include(s => s.Vehicle)
                .Include(s => s.CreatedBy)
                .Include(s => s.Products)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shipment == null)
                throw new Exception("Envío no encontrado");

            // Retornar TU DTO exactamente como lo defines
            return new ShipmentResponseDto
            {
                Id = shipment.Id,
                ShipmentNumber = shipment.ShipmentNumber,
                Status = shipment.Status,
                CreatedAt = shipment.CreatedAt,
                EstimatedDeparture = shipment.EstimatedDeparture,
                ActualDeparture = shipment.ActualDeparture,
                Notes = shipment.Notes,
                StartedAt = shipment.StartedAt,
                ProductCount = shipment.Products?.Count ?? 0,
                TotalQuantity = shipment.Products?.Sum(p => p.Quantity) ?? 0,
                TransportCompany = shipment.TransportCompany != null ? new TransportCompanyDto
                {
                    Id = shipment.TransportCompany.Id,
                    Name = shipment.TransportCompany.Name,
                    IsActive = shipment.TransportCompany.IsActive
                } : null,
                Driver = shipment.Driver != null ? new DriverInfoDto
                {
                    Id = shipment.Driver.Id,
                    IdentificationNumber = shipment.Driver.IdentificationNumber,
                    FullName = shipment.Driver.FullName
                } : null,
                Vehicle = shipment.Vehicle != null ? new VehicleInfoDto
                {
                    Id = shipment.Vehicle.Id,
                    PlateNumber = shipment.Vehicle.PlateNumber,
                    TrailerPlate = shipment.Vehicle.TrailerPlate,
                    VehicleType = shipment.Vehicle.VehicleType
                } : null,
                CreatedBy = shipment.CreatedBy != null ? new UserDto
                {
                    Id = shipment.CreatedBy.Id,
                    Username = shipment.CreatedBy.Username                 
                } : null
            };
        }

        private string GenerateShipmentNumber()
        {
            return $"SH{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }

        // Implementar los demás métodos...
        public async Task<object> StartShipmentAsync(StartShipmentDto dto, int userId)
        {
            var shipment = await _context.Shipments
                .Include(s => s.TransportCompany)
                .Include(s => s.Driver)
                .Include(s => s.Vehicle)
                .FirstOrDefaultAsync(s => s.ShipmentNumber == dto.ShipmentNumber && s.Status == "Pending");

            if (shipment == null)
                throw new Exception("Envío no encontrado o ya está en progreso");

            shipment.Status = "InProgress";
            shipment.StartedAt = DateTime.UtcNow;
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

            return new
            {
                message = "Escaneo iniciado",
                shipmentId = shipment.Id,
                shipmentNumber = shipment.ShipmentNumber,
                transportCompany = shipment.TransportCompany?.Name,
                driverName = shipment.Driver?.FullName,
                vehiclePlate = shipment.Vehicle?.PlateNumber,
                scanOperationId = scanOperation.Id
            };
        }

        public async Task<object> ScanProductAsync(ScanProductDto dto, int userId)
        {
            var shipment = await _context.Shipments
                .Include(s => s.Products)
                .FirstOrDefaultAsync(s => s.Id == dto.ShipmentId && s.Status == "InProgress");

            if (shipment == null)
                throw new Exception("Envío no encontrado o no está en progreso");

            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Barcode == dto.Barcode && p.ShipmentId == dto.ShipmentId);

            string action = "";

            if (existingProduct != null)
            {
                if (!string.IsNullOrEmpty(dto.SerialNumber))
                {
                    var existingWithSameSerial = await _context.Products
                        .FirstOrDefaultAsync(p => p.SerialNumber == dto.SerialNumber && p.ShipmentId == dto.ShipmentId);

                    if (existingWithSameSerial != null)
                        throw new Exception($"Ya existe un producto con el número de serie {dto.SerialNumber}");

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
                        SerialNumber = dto.SerialNumber,
                        ShipmentId = dto.ShipmentId,
                        ScannedAt = DateTime.UtcNow,
                        ScannedByUserId = userId
                    };
                    _context.Products.Add(newProduct);
                    action = "Nuevo producto con serial agregado";
                }
                else
                {
                    existingProduct.Quantity += dto.Quantity;
                    existingProduct.ScannedAt = DateTime.UtcNow;
                    action = "Cantidad incrementada";
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(dto.SerialNumber))
                {
                    var existingSerial = await _context.Products
                        .FirstOrDefaultAsync(p => p.SerialNumber == dto.SerialNumber && p.ShipmentId == dto.ShipmentId);

                    if (existingSerial != null)
                        throw new Exception($"Ya existe un producto con el número de serie {dto.SerialNumber}");
                }

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
                action = "Nuevo producto añadido";
            }

            var scanOperation = await _context.ScanOperations
                .FirstOrDefaultAsync(so => so.ShipmentId == dto.ShipmentId && so.UserId == userId && so.Status == "Active");

            if (scanOperation != null)
            {
                scanOperation.ProductCount = await _context.Products
                    .Where(p => p.ShipmentId == dto.ShipmentId)
                    .SumAsync(p => p.Quantity);
            }

            await _context.SaveChangesAsync();

            var totalAfterScan = await _context.Products
                .Where(p => p.ShipmentId == dto.ShipmentId)
                .SumAsync(p => p.Quantity);

            return new
            {
                message = "Producto escaneado exitosamente",
                shipmentId = dto.ShipmentId,
                barcode = dto.Barcode,
                serialNumber = dto.SerialNumber,
                quantity = dto.Quantity,
                productsCount = totalAfterScan,
                timestamp = DateTime.UtcNow,
                action = action
            };
        }

        public async Task<object> CompleteShipmentAsync(int shipmentId, int userId)
        {
            var shipment = await _context.Shipments
                .Include(s => s.Products)
                .Include(s => s.TransportCompany)
                .FirstOrDefaultAsync(s => s.Id == shipmentId && s.Status == "InProgress");

            if (shipment == null)
                throw new Exception("Envío no encontrado o no está en progreso");

            shipment.Status = "Completed";
            shipment.ActualDeparture = DateTime.UtcNow;
            shipment.UpdatedAt = DateTime.UtcNow;

            var scanOperation = await _context.ScanOperations
                .FirstOrDefaultAsync(so => so.ShipmentId == shipmentId && so.UserId == userId && so.Status == "Active");

            if (scanOperation != null)
            {
                scanOperation.EndTime = DateTime.UtcNow;
                scanOperation.Status = "Completed";
                scanOperation.ProductCount = shipment.Products?.Sum(p => p.Quantity) ?? 0;
            }

            await _context.SaveChangesAsync();

            return new
            {
                message = "Envío completado exitosamente",
                shipmentNumber = shipment.ShipmentNumber,
                totalProducts = shipment.Products?.Sum(p => p.Quantity) ?? 0,
                transportCompany = shipment.TransportCompany?.Name,
                departureTime = shipment.ActualDeparture
            };
        }

        public async Task<List<object>> GetActiveShipmentsAsync()
        {
            var shipments = await _context.Shipments
                .Include(s => s.TransportCompany)
                .Include(s => s.Driver)
                .Include(s => s.Vehicle)
                .Include(s => s.Products)
                .Where(s => s.Status == "InProgress" || s.Status == "Pending")
                .Select(s => new
                {
                    s.Id,
                    s.ShipmentNumber,
                    s.Status,
                    TransportCompany = s.TransportCompany != null ? new { s.TransportCompany.Name } : null,
                    Driver = s.Driver != null ? new { s.Driver.FullName, s.Driver.IdentificationNumber } : null,
                    Vehicle = s.Vehicle != null ? new { s.Vehicle.PlateNumber, s.Vehicle.TrailerPlate, s.Vehicle.VehicleType } : null,
                    ProductCount = s.Products.Sum(p => p.Quantity),
                    s.CreatedAt,
                    s.EstimatedDeparture
                })
                .ToListAsync();

            return shipments.Cast<object>().ToList();
        }

        public async Task<List<object>> GetAllShipmentsAsync()
        {
            var shipments = await _context.Shipments
                .Include(s => s.TransportCompany)
                .Include(s => s.Driver)
                .Include(s => s.Vehicle)
                .Include(s => s.Products)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new
                {
                    s.Id,
                    s.ShipmentNumber,
                    s.Status,
                    TransportCompany = s.TransportCompany != null ? new { s.TransportCompany.Name } : null,
                    Driver = s.Driver != null ? new { s.Driver.FullName, s.Driver.IdentificationNumber } : null,
                    Vehicle = s.Vehicle != null ? new { s.Vehicle.PlateNumber, s.Vehicle.TrailerPlate, s.Vehicle.VehicleType } : null,
                    ProductCount = s.Products.Sum(p => p.Quantity),
                    s.CreatedAt,
                    s.EstimatedDeparture,
                    s.ActualDeparture
                })
                .ToListAsync();

            return shipments.Cast<object>().ToList();
        }

        public async Task<object> GetShipmentStatsAsync()
        {
            var total = await _context.Shipments.CountAsync();
            var pending = await _context.Shipments.CountAsync(s => s.Status == "Pending");
            var inProgress = await _context.Shipments.CountAsync(s => s.Status == "InProgress");
            var completed = await _context.Shipments.CountAsync(s => s.Status == "Completed");
            var cancelled = await _context.Shipments.CountAsync(s => s.Status == "Cancelled");
            var totalProducts = await _context.Products.SumAsync(p => p.Quantity);

            return new
            {
                Total = total,
                Pending = pending,
                InProgress = inProgress,
                Completed = completed,
                Cancelled = cancelled,
                TotalProducts = totalProducts,
                AverageProductsPerShipment = total > 0 ? totalProducts / total : 0,
                Today = await _context.Shipments.CountAsync(s => s.CreatedAt.Date == DateTime.UtcNow.Date),
                ThisWeek = await _context.Shipments.CountAsync(s => s.CreatedAt >= DateTime.UtcNow.AddDays(-7)),
                ThisMonth = await _context.Shipments.CountAsync(s => s.CreatedAt.Month == DateTime.UtcNow.Month && s.CreatedAt.Year == DateTime.UtcNow.Year)
            };
        }

        public async Task<object> UpdateShipmentStatusAsync(int id, UpdateShipmentStatusDto dto, int userId, string userRole)
        {
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null)
                throw new Exception("Envío no encontrado");

            if (shipment.CreatedByUserId != userId && userRole != "Admin")
                throw new UnauthorizedAccessException("No tiene permisos");

            var allowedTransitions = new Dictionary<string, string[]>
            {
                ["Pending"] = new[] { "InProgress", "Cancelled" },
                ["InProgress"] = new[] { "Completed", "Cancelled" },
                ["Completed"] = Array.Empty<string>(),
                ["Cancelled"] = Array.Empty<string>()
            };

            if (!allowedTransitions.ContainsKey(shipment.Status) || !allowedTransitions[shipment.Status].Contains(dto.Status))
                throw new Exception($"No se puede cambiar de '{shipment.Status}' a '{dto.Status}'");

            var previousStatus = shipment.Status;
            shipment.Status = dto.Status;
            shipment.UpdatedAt = DateTime.UtcNow;

            if (dto.Status == "Completed" && !shipment.ActualDeparture.HasValue)
                shipment.ActualDeparture = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new
            {
                message = $"Estado actualizado a '{dto.Status}'",
                shipmentId = shipment.Id,
                previousStatus = previousStatus,
                newStatus = dto.Status,
                updatedAt = shipment.UpdatedAt
            };
        }

        public async Task<object> CancelShipmentAsync(int id)
        {
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null)
                throw new Exception("Envío no encontrado");

            if (shipment.Status != "Pending" && shipment.Status != "InProgress")
                throw new Exception($"No se puede cancelar un envío en estado '{shipment.Status}'");

            shipment.Status = "Cancelled";
            shipment.UpdatedAt = DateTime.UtcNow;

            var activeScan = await _context.ScanOperations
                .FirstOrDefaultAsync(so => so.ShipmentId == id && so.Status == "Active");

            if (activeScan != null)
            {
                activeScan.EndTime = DateTime.UtcNow;
                activeScan.Status = "Cancelled";
            }

            await _context.SaveChangesAsync();

            return new
            {
                message = "Envío cancelado exitosamente",
                shipmentNumber = shipment.ShipmentNumber,
                cancelledAt = DateTime.UtcNow
            };
        }
    }
}