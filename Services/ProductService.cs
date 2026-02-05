using Microsoft.EntityFrameworkCore;
using BarcodeShippingSystem.Data;
using BarcodeShippingSystem.DTOs;
using BarcodeShippingSystem.Models;
using BarcodeShippingSystem.Services;
using Microsoft.Extensions.Logging;

namespace BarcodeShippingSystem.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductService> _logger;

        public ProductService(ApplicationDbContext context, ILogger<ProductService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ============== MÉTODOS IMPLEMENTADOS ==============

        // Método principal para procesar escaneo
        public async Task<ScanResponseDto> ProcessScanAsync(ScanProductDto scanDto, int userId)
        {
            try
            {
                // 1. Verificar que el envío existe y está activo
                var shipment = await _context.Shipments
                    .Include(s => s.Products)
                    .Include(s => s.TransportCompany)
                    .FirstOrDefaultAsync(s => s.Id == scanDto.ShipmentId);

                if (shipment == null)
                {
                    return new ScanResponseDto
                    {
                        Success = false,
                        Message = $"🚫 Envío con ID {scanDto.ShipmentId} no encontrado"
                    };
                }

                // Verificar estado del envío
                if (shipment.Status != "Pending" && shipment.Status != "InProgress")
                {
                    return new ScanResponseDto
                    {
                        Success = false,
                        Message = $"⏸️ El envío está en estado '{shipment.Status}'. Solo se pueden escanear envíos Pendientes o En Progreso."
                    };
                }

                // 2. Buscar si ya existe producto con este código de barras
                var existingProduct = await _context.Products
                    .FirstOrDefaultAsync(p => p.Barcode == scanDto.Barcode);

                Product product;

                if (existingProduct != null)
                {
                    // Producto ya existe - actualizar
                    product = existingProduct;

                    // Actualizar campos si se proporcionan
                    if (!string.IsNullOrEmpty(scanDto.Name))
                        product.Name = scanDto.Name;

                    if (!string.IsNullOrEmpty(scanDto.Description))
                        product.Description = scanDto.Description;

                    if (!string.IsNullOrEmpty(scanDto.SKU))
                        product.SKU = scanDto.SKU;

                    if (!string.IsNullOrEmpty(scanDto.Category))
                        product.Category = scanDto.Category;

                    if (!string.IsNullOrEmpty(scanDto.Model))
                        product.Model = scanDto.Model;

                    if (!string.IsNullOrEmpty(scanDto.SerialNumber))
                        product.SerialNumber = scanDto.SerialNumber;

                    // Incrementar cantidad si es el mismo producto
                    product.Quantity += scanDto.Quantity;

                    product.ShipmentId = scanDto.ShipmentId;
                    product.ScannedAt = DateTime.UtcNow;
                    product.ScannedByUserId = userId;

                    _context.Products.Update(product);
                    _logger.LogInformation("Producto actualizado: {Barcode} (Cantidad: {Quantity})",
                        product.Barcode, product.Quantity);
                }
                else
                {
                    // Producto nuevo - crear
                    product = new Product
                    {
                        Barcode = scanDto.Barcode.Trim(),
                        Name = scanDto.Name?.Trim() ?? "Producto Samsung",
                        Description = scanDto.Description?.Trim(),
                        SKU = scanDto.SKU?.Trim() ?? GenerateSku(scanDto.Barcode),
                        Quantity = scanDto.Quantity,
                        Category = scanDto.Category?.Trim() ?? "Electrónica",
                        Brand = "Samsung", // Siempre Samsung según tu modelo
                        Model = scanDto.Model?.Trim(),
                        SerialNumber = scanDto.SerialNumber?.Trim(),
                        ShipmentId = scanDto.ShipmentId,
                        ScannedAt = DateTime.UtcNow,
                        ScannedByUserId = userId
                    };

                    _context.Products.Add(product);
                    _logger.LogInformation("Producto creado: {Barcode} - {Name}",
                        product.Barcode, product.Name);
                }

                // 3. Si el envío está Pendiente, cambiarlo a En Progreso
                if (shipment.Status == "Pending")
                {
                    shipment.Status = "InProgress";
                    shipment.StartedAt = DateTime.UtcNow;
                    _context.Shipments.Update(shipment);
                }

                await _context.SaveChangesAsync();

                // 4. Obtener estadísticas actualizadas
                var shipmentProductCount = await _context.Products
                    .CountAsync(p => p.ShipmentId == scanDto.ShipmentId);

                var categoryCounts = await GetShipmentCategoryCountsAsync(scanDto.ShipmentId);

                // 5. Preparar respuesta
                var productDto = new ProductDto
                {
                    Id = product.Id,
                    Barcode = product.Barcode,
                    Name = product.Name,
                    Description = product.Description,
                    SKU = product.SKU,
                    Quantity = product.Quantity,
                    Category = product.Category,
                    Brand = product.Brand,
                    Model = product.Model,
                    SerialNumber = product.SerialNumber,
                    ShipmentId = product.ShipmentId,
                    ScannedAt = product.ScannedAt,
                    ScannedByUserId = product.ScannedByUserId,
                    ShipmentNumber = shipment.ShipmentNumber
                };

                return new ScanResponseDto
                {
                    Success = true,
                    Message = $"✅ Producto escaneado exitosamente: {product.Name}",
                    Product = productDto,
                    TotalScanned = shipmentProductCount,
                    ShipmentProductCount = shipmentProductCount,
                    ProductName = product.Name,
                    ProductCategory = product.Category,
                    CategoryCounts = categoryCounts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando escaneo: {Barcode}", scanDto.Barcode);
                return new ScanResponseDto
                {
                    Success = false,
                    Message = $"❌ Error al procesar el escaneo: {ex.Message}"
                };
            }
        }

        // Método para escanear producto (alias para compatibilidad)
        public async Task<ScanResponseDto> ScanProductAsync(ScanProductDto scanDto, int userId)
        {
            return await ProcessScanAsync(scanDto, userId);
        }

        // ============== MÉTODOS FALTANTES ==============

        // Obtener producto por ID
        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            return await _context.Products
                .Where(p => p.Id == id)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Barcode = p.Barcode,
                    Name = p.Name,
                    Description = p.Description,
                    SKU = p.SKU,
                    Quantity = p.Quantity,
                    Category = p.Category,
                    Brand = p.Brand,
                    Model = p.Model,
                    SerialNumber = p.SerialNumber,
                    ShipmentId = p.ShipmentId,
                    ScannedAt = p.ScannedAt,
                    ScannedByUserId = p.ScannedByUserId,
                    ScannedByUserName = p.ScannedByUser != null ? p.ScannedByUser.Username : null,
                    ShipmentNumber = p.Shipment != null ? p.Shipment.ShipmentNumber : null
                })
                .FirstOrDefaultAsync();
        }

        // Obtener todos los productos
        public async Task<List<ProductDto>> GetAllAsync()
        {
            return await _context.Products
                .OrderByDescending(p => p.ScannedAt)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Barcode = p.Barcode,
                    Name = p.Name,
                    Description = p.Description,
                    SKU = p.SKU,
                    Quantity = p.Quantity,
                    Category = p.Category,
                    Brand = p.Brand,
                    Model = p.Model,
                    SerialNumber = p.SerialNumber,
                    ShipmentId = p.ShipmentId,
                    ScannedAt = p.ScannedAt,
                    ScannedByUserId = p.ScannedByUserId,
                    ScannedByUserName = p.ScannedByUser != null ? p.ScannedByUser.Username : null,
                    ShipmentNumber = p.Shipment != null ? p.Shipment.ShipmentNumber : null
                })
                .ToListAsync();
        }

        // Buscar productos con filtros
        public async Task<PagedResponseDto<ProductDto>> SearchAsync(ProductSearchDto searchDto)
        {
            var query = _context.Products.AsQueryable();

            // Aplicar filtros
            if (!string.IsNullOrWhiteSpace(searchDto.Barcode))
                query = query.Where(p => p.Barcode.Contains(searchDto.Barcode));

            if (!string.IsNullOrWhiteSpace(searchDto.Name))
                query = query.Where(p => p.Name.Contains(searchDto.Name));

            if (!string.IsNullOrWhiteSpace(searchDto.Category))
                query = query.Where(p => p.Category == searchDto.Category);

            if (!string.IsNullOrWhiteSpace(searchDto.SKU))
                query = query.Where(p => p.SKU.Contains(searchDto.SKU));

            if (!string.IsNullOrWhiteSpace(searchDto.SerialNumber))
                query = query.Where(p => p.SerialNumber != null && p.SerialNumber.Contains(searchDto.SerialNumber));

            if (searchDto.ShipmentId.HasValue)
                query = query.Where(p => p.ShipmentId == searchDto.ShipmentId);

            if (searchDto.StartDate.HasValue)
                query = query.Where(p => p.ScannedAt >= searchDto.StartDate.Value);

            if (searchDto.EndDate.HasValue)
                query = query.Where(p => p.ScannedAt <= searchDto.EndDate.Value);

            // Ordenar
            query = query.OrderByDescending(p => p.ScannedAt);

            // Obtener total
            var totalCount = await query.CountAsync();

            // Paginar
            var items = await query
                .Skip((searchDto.Page - 1) * searchDto.PageSize)
                .Take(searchDto.PageSize)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Barcode = p.Barcode,
                    Name = p.Name,
                    Description = p.Description,
                    SKU = p.SKU,
                    Quantity = p.Quantity,
                    Category = p.Category,
                    Brand = p.Brand,
                    Model = p.Model,
                    SerialNumber = p.SerialNumber,
                    ShipmentId = p.ShipmentId,
                    ScannedAt = p.ScannedAt,
                    ScannedByUserId = p.ScannedByUserId,
                    ScannedByUserName = p.ScannedByUser != null ? p.ScannedByUser.Username : null,
                    ShipmentNumber = p.Shipment != null ? p.Shipment.ShipmentNumber : null
                })
                .ToListAsync();

            return new PagedResponseDto<ProductDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = searchDto.Page,
                PageSize = searchDto.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)searchDto.PageSize)
            };
        }

        // Crear producto
        public async Task<ProductDto> CreateAsync(CreateProductDto dto, int? userId = null)
        {
            // Verificar si ya existe producto con ese código de barras
            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Barcode == dto.Barcode);

            if (existingProduct != null)
                throw new InvalidOperationException($"Ya existe un producto con el código de barras: {dto.Barcode}");

            var product = new Product
            {
                Barcode = dto.Barcode.Trim(),
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                SKU = dto.SKU.Trim(),
                Quantity = dto.Quantity,
                Category = dto.Category.Trim(),
                Brand = dto.Brand?.Trim() ?? "Samsung",
                Model = dto.Model?.Trim(),
                SerialNumber = dto.SerialNumber?.Trim(),
                ScannedAt = DateTime.UtcNow,
                ScannedByUserId = userId
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Producto creado: {Barcode} - {Name}", product.Barcode, product.Name);

            return await GetByIdAsync(product.Id) ?? throw new Exception("Error al obtener producto creado");
        }

        // Actualizar producto
        public async Task<ProductDto?> UpdateAsync(int id, UpdateProductDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return null;

            // Actualizar solo los campos que se proporcionaron
            if (!string.IsNullOrEmpty(dto.Name))
                product.Name = dto.Name.Trim();

            if (dto.Description != null)
                product.Description = dto.Description.Trim();

            if (!string.IsNullOrEmpty(dto.Category))
                product.Category = dto.Category.Trim();

            if (!string.IsNullOrEmpty(dto.SKU))
                product.SKU = dto.SKU.Trim();

            if (dto.Quantity.HasValue)
                product.Quantity = dto.Quantity.Value;

            if (!string.IsNullOrEmpty(dto.Brand))
                product.Brand = dto.Brand.Trim();

            if (dto.Model != null)
                product.Model = dto.Model.Trim();

            if (dto.SerialNumber != null)
                product.SerialNumber = dto.SerialNumber.Trim();

            product.ScannedAt = DateTime.UtcNow;

            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        // Eliminar producto
        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Producto eliminado: ID {Id} - {Barcode}", id, product.Barcode);
            return true;
        }

        // ============== MÉTODOS ADICIONALES ==============

        // Obtener productos por envío
        public async Task<List<ProductDto>> GetProductsByShipmentAsync(int shipmentId)
        {
            return await _context.Products
                .Where(p => p.ShipmentId == shipmentId)
                .OrderByDescending(p => p.ScannedAt)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Barcode = p.Barcode,
                    Name = p.Name,
                    Description = p.Description,
                    SKU = p.SKU,
                    Quantity = p.Quantity,
                    Category = p.Category,
                    Brand = p.Brand,
                    Model = p.Model,
                    SerialNumber = p.SerialNumber,
                    ShipmentId = p.ShipmentId,
                    ScannedAt = p.ScannedAt,
                    ScannedByUserId = p.ScannedByUserId,
                    ShipmentNumber = p.Shipment != null ? p.Shipment.ShipmentNumber : null
                })
                .ToListAsync();
        }

        // Obtener productos por código de barras
        public async Task<List<ProductDto>> GetProductsByBarcodeAsync(string barcode)
        {
            return await _context.Products
                .Where(p => p.Barcode.Contains(barcode))
                .OrderByDescending(p => p.ScannedAt)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Barcode = p.Barcode,
                    Name = p.Name,
                    Description = p.Description,
                    SKU = p.SKU,
                    Quantity = p.Quantity,
                    Category = p.Category,
                    Brand = p.Brand,
                    Model = p.Model,
                    SerialNumber = p.SerialNumber,
                    ShipmentId = p.ShipmentId,
                    ScannedAt = p.ScannedAt,
                    ScannedByUserId = p.ScannedByUserId,
                    ShipmentNumber = p.Shipment != null ? p.Shipment.ShipmentNumber : null
                })
                .ToListAsync();
        }

        // Verificar si producto existe
        public async Task<bool> ProductExistsAsync(string barcode)
        {
            return await _context.Products.AnyAsync(p => p.Barcode == barcode);
        }

        // Obtener producto por código de barras (modelo)
        public async Task<Product?> GetProductByBarcodeAsync(string barcode)
        {
            return await _context.Products
                .FirstOrDefaultAsync(p => p.Barcode == barcode);
        }

        // Obtener estadísticas
        public async Task<ProductStatsDto> GetStatsAsync()
        {
            var today = DateTime.UtcNow.Date;

            var stats = new ProductStatsDto
            {
                TotalProducts = await _context.Products.CountAsync(),
                ProductsScannedToday = await _context.Products
                    .CountAsync(p => p.ScannedAt.Date == today),
                ProductsByCategory = await _context.Products
                    .GroupBy(p => p.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Category, x => x.Count),
                ProductsByBrand = await _context.Products
                    .GroupBy(p => p.Brand)
                    .Select(g => new { Brand = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Brand, x => x.Count)
            };

            return stats;
        }

        // Obtener conteos por categoría
        public async Task<Dictionary<string, int>> GetCategoryCountsAsync()
        {
            return await _context.Products
                .GroupBy(p => p.Category)
                    .Select(g => new { Category = g.Key, Count = g.Sum(p => p.Quantity) })
                    .ToDictionaryAsync(x => x.Category, x => x.Count);
        }

        // Generar SKU automático
        private string GenerateSku(string barcode)
        {
            return $"SAMSUNG-{DateTime.UtcNow:yyyyMMdd}-{barcode.Substring(Math.Max(0, barcode.Length - 6))}";
        }

        // Obtener conteos por categoría para un envío específico
        public async Task<Dictionary<string, int>> GetShipmentCategoryCountsAsync(int shipmentId)
        {
            var counts = await _context.Products
                .Where(p => p.ShipmentId == shipmentId)
                .GroupBy(p => p.Category)
                .Select(g => new { Category = g.Key, Count = g.Sum(p => p.Quantity) })
                .ToListAsync();

            return counts.ToDictionary(x => x.Category, x => x.Count);
        }

        // Contar productos totales
        public async Task<int> GetTotalProductsAsync()
        {
            return await _context.Products.CountAsync();
        }

        // Contar productos por envío
        public async Task<int> GetProductsCountByShipmentAsync(int shipmentId)
        {
            return await _context.Products
                .Where(p => p.ShipmentId == shipmentId)
                .SumAsync(p => p.Quantity);
        }

        // Obtener productos escaneados hoy
        public async Task<List<ProductDto>> GetProductsScannedTodayAsync()
        {
            var today = DateTime.UtcNow.Date;

            return await _context.Products
                .Where(p => p.ScannedAt.Date == today)
                .OrderByDescending(p => p.ScannedAt)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Barcode = p.Barcode,
                    Name = p.Name,
                    Description = p.Description,
                    SKU = p.SKU,
                    Quantity = p.Quantity,
                    Category = p.Category,
                    Brand = p.Brand,
                    Model = p.Model,
                    SerialNumber = p.SerialNumber,
                    ShipmentId = p.ShipmentId,
                    ScannedAt = p.ScannedAt,
                    ScannedByUserId = p.ScannedByUserId,
                    ShipmentNumber = p.Shipment != null ? p.Shipment.ShipmentNumber : null
                })
                .ToListAsync();
        }

        // Obtener productos sin envío asignado
        public async Task<List<ProductDto>> GetProductsWithoutShipmentAsync()
        {
            return await _context.Products
                .Where(p => p.ShipmentId == null)
                .OrderByDescending(p => p.ScannedAt)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Barcode = p.Barcode,
                    Name = p.Name,
                    Description = p.Description,
                    SKU = p.SKU,
                    Quantity = p.Quantity,
                    Category = p.Category,
                    Brand = p.Brand,
                    Model = p.Model,
                    SerialNumber = p.SerialNumber,
                    ShipmentId = p.ShipmentId,
                    ScannedAt = p.ScannedAt,
                    ScannedByUserId = p.ScannedByUserId,
                    ShipmentNumber = p.Shipment != null ? p.Shipment.ShipmentNumber : null
                })
                .ToListAsync();
        }

        // Obtener productos por categoría
        public async Task<List<ProductDto>> GetProductsByCategoryAsync(string category)
        {
            return await _context.Products
                .Where(p => p.Category == category)
                .OrderByDescending(p => p.ScannedAt)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Barcode = p.Barcode,
                    Name = p.Name,
                    Description = p.Description,
                    SKU = p.SKU,
                    Quantity = p.Quantity,
                    Category = p.Category,
                    Brand = p.Brand,
                    Model = p.Model,
                    SerialNumber = p.SerialNumber,
                    ShipmentId = p.ShipmentId,
                    ScannedAt = p.ScannedAt,
                    ScannedByUserId = p.ScannedByUserId,
                    ShipmentNumber = p.Shipment != null ? p.Shipment.ShipmentNumber : null
                })
                .ToListAsync();
        }
    }
}