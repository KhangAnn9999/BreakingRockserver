using GameInventoryApi.DTOs;

namespace GameInventoryApi.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);
    // Thêm dòng này để khai báo hàm Register
    Task<bool> RegisterAsync(RegisterDto registerDto);
}