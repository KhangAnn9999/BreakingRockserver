using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GameInventoryApi.DTOs;
using GameInventoryApi.Models;
using GameInventoryApi.Repositories;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GameInventoryApi.Services;

public class AuthService : IAuthService
{
    private readonly IMongoRepository<User> _userRepository;
    // Inject thêm repository của PlayerProfile để lưu tiền khi đăng ký
    private readonly IMongoRepository<PlayerProfile> _profileRepository; 
    private readonly JwtSettings _jwtSettings;

    // Sửa hàm khởi tạo để nhận cả 2 Repository
    public AuthService(
        IMongoRepository<User> userRepository, 
        IMongoRepository<PlayerProfile> profileRepository,
        IOptions<JwtSettings> jwtSettings)
    {
        _userRepository = userRepository;
        _profileRepository = profileRepository;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
    {
        var user = await _userRepository.GetByFilterAsync(u => u.Username == loginDto.Username);
        if (user == null || user.PasswordHash != loginDto.Password) return null;

        var token = GenerateJwtToken(user);
        return new AuthResponseDto(Token: token, Role: user.Role, Username: user.Username);
    }

    // === THÊM HÀM REGISTER VÀO ĐÂY ===
    public async Task<bool> RegisterAsync(RegisterDto registerDto)
    {
        // 1. Kiểm tra xem Username đã tồn tại trong DB chưa
        var existingUser = await _userRepository.GetByFilterAsync(u => u.Username == registerDto.Username);
        if (existingUser != null) return false; // Trùng tên, không cho đăng ký

        // 2. Tạo tài khoản User mới (Lưu password dạng thô theo logic Login cũ của bạn)
        var newUser = new User
        {
            Username = registerDto.Username,
            PasswordHash = registerDto.Password,
            Role = "Player" // Mặc định là người chơi
        };
        await _userRepository.CreateAsync(newUser);

        // 3. Tự động tạo hồ sơ nhân vật mới tinh và cấp luôn 100 Gold khởi nghiệp
        var newProfile = new PlayerProfile
        {
            PlayerId = newUser.Id, // Link ID của User vừa tạo sang hồ sơ
            Username = newUser.Username,
            Level = 1,
            Experience = 0,
            Gold = 100 // Số tiền khởi điểm bạn muốn nạp vào BE
        };
        await _profileRepository.CreateAsync(newProfile);

        return true;
    }

    private string GenerateJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("PlayerId", user.Id ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}