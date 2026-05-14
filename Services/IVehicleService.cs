using BarcodeShippingSystem.DTOs;

namespace BarcodeShippingSystem.Services
{
    public interface IVehicleService
    {
        Task<object> GetByCompanyAsync(int companyId);
        Task<object> CreateAsync(CreateVehicleDto dto);
    }
}