using BarcodeShippingSystem.DTOs;

namespace BarcodeShippingSystem.Services
{
    public interface IDriverService
    {
        Task<object> GetByCompanyAsync(int companyId);
        Task<object> CreateAsync(CreateDriverDto dto);
        Task<object> LinkToCompanyAsync(int driverId, int transportCompanyId);
        Task<object> GetUnlinkedDriversAsync();
    }
}