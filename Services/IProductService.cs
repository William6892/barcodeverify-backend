using BarcodeShippingSystem.DTOs;
using BarcodeShippingSystem.Models;

namespace BarcodeShippingSystem.Services
{
    public interface IProductService
    {
        // Operaciones básicas
        Task<ProductDto?> GetByIdAsync(int id);
        Task<List<ProductDto>> GetAllAsync();
        Task<PagedResponseDto<ProductDto>> SearchAsync(ProductSearchDto searchDto);
        Task<ProductDto> CreateAsync(CreateProductDto dto, int? userId = null);
        Task<ProductDto?> UpdateAsync(int id, UpdateProductDto dto);
        Task<bool> DeleteAsync(int id);

        // Operaciones de escaneo
        Task<ScanResponseDto> ScanProductAsync(ScanProductDto scanDto, int userId);
        Task<List<ProductDto>> GetProductsByShipmentAsync(int shipmentId);
        Task<List<ProductDto>> GetProductsByBarcodeAsync(string barcode);

        // Estadísticas
        Task<ProductStatsDto> GetStatsAsync();
        Task<Dictionary<string, int>> GetCategoryCountsAsync();

        // Validaciones
        Task<bool> ProductExistsAsync(string barcode);
        Task<Product?> GetProductByBarcodeAsync(string barcode);

        // Nuevos métodos para tu ScanProductDto
        Task<ScanResponseDto> ProcessScanAsync(ScanProductDto scanDto, int userId);
        Task<Dictionary<string, int>> GetShipmentCategoryCountsAsync(int shipmentId);
    }
}