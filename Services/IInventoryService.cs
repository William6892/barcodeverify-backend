using BarcodeShippingSystem.DTOs;

namespace BarcodeShippingSystem.Services
{
    public interface IInventoryService
    {
        // ✅ Este lo llamas desde RegisterInventoryExit
        Task RecordExitAsync(int productId, int quantity, int referenceId, string referenceType, int userId, string? notes = null);

        // ✅ Este lo llamas desde InitializeInventoryForProduct
        Task InitializeInventoryAsync(int productId, int initialStock, int userId);

        // ✅ Para obtener stock actual
        Task<int> GetCurrentStockAsync(int productId);

        // ✅ Para reporte diario
        Task<List<DailyStockDto>> GetDailyStockSummaryAsync(DateTime date);
    }
}