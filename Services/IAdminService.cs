// Services/IAdminService.cs
namespace BarcodeShippingSystem.Services
{
    public interface IAdminService
    {
        Task<object> GetDashboardStatsAsync(DateTime? startDate, DateTime? endDate);
        Task<object> GetAllUsersAsync();
        Task<object> GetAllTransportCompaniesAsync();
        Task<object> SearchProductsAsync(string? barcode, string? name, string? category, DateTime? startDate, DateTime? endDate, int page, int pageSize);
        Task<object> GenerateShipmentReportAsync(DateTime? startDate, DateTime? endDate);
        Task<object> GetQuickStatsAsync();
    }
}