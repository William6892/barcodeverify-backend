// Services/IShipmentService.cs
using BarcodeShippingSystem.DTOs;

namespace BarcodeShippingSystem.Services
{
    public interface IShipmentService
    {
        Task<ShipmentResponseDto> CreateShipmentAsync(CreateShipmentDto dto, int userId);
        Task<object> StartShipmentAsync(StartShipmentDto dto, int userId);
        Task<object> ScanProductAsync(ScanProductDto dto, int userId);
        Task<object> CompleteShipmentAsync(int shipmentId, int userId);
        Task<object> UpdateShipmentStatusAsync(int id, UpdateShipmentStatusDto dto, int userId, string userRole);
        Task<object> CancelShipmentAsync(int id);
        Task<List<object>> GetAllShipmentsAsync();
        Task<List<object>> GetActiveShipmentsAsync();
        Task<ShipmentResponseDto> GetShipmentByIdAsync(int id);
        Task<object> GetShipmentStatsAsync();
    }
}