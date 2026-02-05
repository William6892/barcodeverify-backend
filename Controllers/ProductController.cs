using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BarcodeShippingSystem.DTOs;
using BarcodeShippingSystem.Services;

namespace BarcodeShippingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IProductService productService, ILogger<ProductController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        // GET: api/Product
        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                var products = await _productService.GetAllAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo todos los productos");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // GET: api/Product/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            try
            {
                var product = await _productService.GetByIdAsync(id);
                if (product == null)
                    return NotFound(new { message = "Producto no encontrado" });

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo producto ID: {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // POST: api/Product/scan
        [HttpPost("scan")]
        public async Task<IActionResult> ScanProduct([FromBody] ScanProductDto scanDto)
        {
            try
            {
                // Obtener userId del token JWT
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                if (userId == 0)
                    return Unauthorized(new { message = "Usuario no autenticado" });

                var result = await _productService.ProcessScanAsync(scanDto, userId);

                if (!result.Success)
                    return BadRequest(new { result.Message });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error escaneando producto: {Barcode}", scanDto.Barcode);
                return StatusCode(500, new
                {
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        // GET: api/Product/shipment/{shipmentId}
        [HttpGet("shipment/{shipmentId}")]
        public async Task<IActionResult> GetProductsByShipment(int shipmentId)
        {
            try
            {
                var products = await _productService.GetProductsByShipmentAsync(shipmentId);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo productos del envío: {ShipmentId}", shipmentId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // GET: api/Product/search
        [HttpGet("search")]
        public async Task<IActionResult> SearchProducts([FromQuery] ProductSearchDto searchDto)
        {
            try
            {
                var result = await _productService.SearchAsync(searchDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error buscando productos");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // GET: api/Product/barcode/{barcode}
        [HttpGet("barcode/{barcode}")]
        public async Task<IActionResult> GetProductsByBarcode(string barcode)
        {
            try
            {
                var products = await _productService.GetProductsByBarcodeAsync(barcode);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo productos por código: {Barcode}", barcode);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // GET: api/Product/stats
        [HttpGet("stats")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetProductStats()
        {
            try
            {
                var stats = await _productService.GetStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estadísticas de productos");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // GET: api/Product/shipment/{shipmentId}/categories
        [HttpGet("shipment/{shipmentId}/categories")]
        public async Task<IActionResult> GetShipmentCategoryCounts(int shipmentId)
        {
            try
            {
                var counts = await _productService.GetShipmentCategoryCountsAsync(shipmentId);
                return Ok(counts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo conteos por categoría para envío: {ShipmentId}", shipmentId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // POST: api/Product
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var product = await _productService.CreateAsync(dto, userId);
                return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando producto");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // PUT: api/Product/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto dto)
        {
            try
            {
                var product = await _productService.UpdateAsync(id, dto);
                if (product == null)
                    return NotFound(new { message = "Producto no encontrado" });

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando producto ID: {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // DELETE: api/Product/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var result = await _productService.DeleteAsync(id);
                if (!result)
                    return NotFound(new { message = "Producto no encontrado" });

                return Ok(new { message = "Producto eliminado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando producto ID: {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }


        // ✅ AGREGAR ESTE MÉTODO NUEVO al final del ProductController
        // POST: api/Product/create-for-shipment
        [HttpPost("create-for-shipment")]
        [Authorize] // Solo requiere autenticación, no rol Admin
        public async Task<IActionResult> CreateProductForShipment([FromBody] CreateProductForShipmentDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                if (userId == 0)
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Usuario no autenticado"
                    });

                // Validar si ya existe el producto en este envío
                var existingProducts = await _productService.GetProductsByShipmentAsync(dto.ShipmentId);
                var existingProduct = existingProducts.FirstOrDefault(p => p.Barcode == dto.Barcode);

                if (existingProduct != null)
                {
                    return Conflict(new
                    {
                        success = false,
                        message = $"El producto {dto.Barcode} ya existe en este envío"
                    });
                }

                // Crear el DTO para el servicio
                var createDto = new CreateProductDto
                {
                    Barcode = dto.Barcode,
                    Name = dto.Name,
                    Quantity = dto.Quantity,
                    Category = dto.Category ?? "General",
                    SKU = $"SHIP-{dto.ShipmentId}-{dto.Barcode}", // SKU generado automáticamente
                    Description = $"Producto agregado al envío {dto.ShipmentId}",
                    Brand = "General" // Valor por defecto
                };

                // Llamar al servicio para crear el producto
                var product = await _productService.CreateAsync(createDto, userId);

                // Asociar el producto al envío si es necesario
                // Esto depende de cómo esté implementado tu servicio
                // Puede que necesites un método adicional como _productService.AssignToShipmentAsync(product.Id, dto.ShipmentId);

                return Ok(new
                {
                    success = true,
                    message = "Producto creado exitosamente para el envío",
                    product = new
                    {
                        id = product.Id,
                        barcode = product.Barcode,
                        name = product.Name,
                        quantity = product.Quantity,
                        category = product.Category,
                        shipmentId = dto.ShipmentId,
                        createdAt = DateTime.UtcNow
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando producto para envío. Barcode: {Barcode}, ShipmentId: {ShipmentId}",
                    dto.Barcode, dto.ShipmentId);

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor al crear producto",
                    error = ex.Message
                });
            }
        }

    }
}