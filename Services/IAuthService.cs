using BarcodeShippingSystem.DTOs;

namespace BarcodeShippingSystem.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<UserDto> RegisterAsync(RegisterDto registerDto);
        Task<UserDto> CreateUserAsync(RegisterDto registerDto, int currentUserId);
        Task<UserDto> GetCurrentUserAsync(int userId);
    }
}