using System.ComponentModel.DataAnnotations;

namespace BarcodeShippingSystem.DTOs
{
    // DTO para crear producto
    public class CreateProductDto
    {
        [Required(ErrorMessage = "El código de barras es requerido")]
        [StringLength(100, MinimumLength = 4, ErrorMessage = "El código debe tener entre 4 y 100 caracteres")]
        public string Barcode { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del producto es requerido")]
        [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "La categoría es requerida")]
        [StringLength(100, ErrorMessage = "La categoría no puede exceder 100 caracteres")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "El SKU es requerido")]
        [StringLength(50, ErrorMessage = "El SKU no puede exceder 50 caracteres")]
        public string SKU { get; set; } = string.Empty;

        [Range(1, 1000, ErrorMessage = "La cantidad debe estar entre 1 y 1000")]
        public int Quantity { get; set; } = 1;

        [StringLength(50, ErrorMessage = "La marca no puede exceder 50 caracteres")]
        public string Brand { get; set; } = "Samsung";

        [StringLength(50, ErrorMessage = "El modelo no puede exceder 50 caracteres")]
        public string? Model { get; set; }

        [StringLength(100, ErrorMessage = "El número de serie no puede exceder 100 caracteres")]
        public string? SerialNumber { get; set; }
    }

    // DTO para actualizar producto
    public class UpdateProductDto
    {
        [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        public string? Name { get; set; }

        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string? Description { get; set; }

        [StringLength(100, ErrorMessage = "La categoría no puede exceder 100 caracteres")]
        public string? Category { get; set; }

        [StringLength(50, ErrorMessage = "El SKU no puede exceder 50 caracteres")]
        public string? SKU { get; set; }

        [Range(1, 1000, ErrorMessage = "La cantidad debe estar entre 1 y 1000")]
        public int? Quantity { get; set; }

        [StringLength(50, ErrorMessage = "La marca no puede exceder 50 caracteres")]
        public string? Brand { get; set; }

        [StringLength(50, ErrorMessage = "El modelo no puede exceder 50 caracteres")]
        public string? Model { get; set; }

        [StringLength(100, ErrorMessage = "El número de serie no puede exceder 100 caracteres")]
        public string? SerialNumber { get; set; }
    }

    // DTO para respuesta de producto
    public class ProductDto
    {
        public int Id { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string SKU { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }
        public int? ShipmentId { get; set; }
        public DateTime ScannedAt { get; set; }
        public int? ScannedByUserId { get; set; }
        public string? ScannedByUserName { get; set; }
        public string? ShipmentNumber { get; set; }
        public string? Type { get; set; }
        public bool HasAccessories { get; set; }
    }

    // YA EXISTENTE - DTO para escaneo de producto en envío
    public class ScanProductDto
    {
        [Required]
        public int ShipmentId { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string Barcode { get; set; }

        [Range(1, 1000)]
        public int Quantity { get; set; } = 1;

        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? SKU { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        [StringLength(100)]
        public string? Model { get; set; }

        // ✅ Validación especial para número de serie
        [StringLength(100)]
        [RegularExpression(@"^[A-Za-z0-9\-_]+$", ErrorMessage = "Solo letras, números, guiones y guiones bajos")]
        public string? SerialNumber { get; set; }
    }

    // DTO para respuesta de escaneo
    public class ScanResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ProductDto? Product { get; set; }
        public int TotalScanned { get; set; }
        public int ShipmentProductCount { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCategory { get; set; }
        public Dictionary<string, int> CategoryCounts { get; set; } = new();
    }

    // DTO para estadísticas de productos
    public class ProductStatsDto
    {
        public int TotalProducts { get; set; }
        public int TotalScanned { get; set; }
        public int ActiveProducts { get; set; }
        public Dictionary<string, int> ProductsByCategory { get; set; } = new();
        public Dictionary<string, int> ProductsByBrand { get; set; } = new();
        public int ProductsScannedToday { get; set; }
        public Dictionary<string, int> ProductsByType { get; set; } = new();
    }

    // DTO para búsqueda de productos
    public class ProductSearchDto
    {
        public string? Barcode { get; set; }
        public string? Name { get; set; }
        public string? Category { get; set; }
        public string? SKU { get; set; }
        public string? SerialNumber { get; set; }
        public string? Type { get; set; }
        public int? ShipmentId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // DTO para respuesta paginada
    public class PagedResponseDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class CreateProductForShipmentDto
    {
        [Required(ErrorMessage = "El código de barras es requerido")]
        [StringLength(50, MinimumLength = 4, ErrorMessage = "El código debe tener entre 4 y 50 caracteres")]
        public string Barcode { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del producto es requerido")]
        [StringLength(200, ErrorMessage = "El nombre no puede exceder los 200 caracteres")]
        public string Name { get; set; } = string.Empty;

        [Range(1, 999, ErrorMessage = "La cantidad debe estar entre 1 y 999")]
        public int Quantity { get; set; } = 1;

        [StringLength(100, ErrorMessage = "La categoría no puede exceder los 100 caracteres")]
        public string? Category { get; set; }

        [Required(ErrorMessage = "El ID del envío es requerido")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del envío debe ser válido")]
        public int ShipmentId { get; set; }
    }
}