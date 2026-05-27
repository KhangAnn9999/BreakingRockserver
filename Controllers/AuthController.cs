using Microsoft.AspNetCore.Mvc;
using GameInventoryApi.DTOs;
using GameInventoryApi.Services;

namespace GameInventoryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
    {
        var response = await _authService.LoginAsync(loginDto);
        return response != null ? Ok(response) : Unauthorized();
    }
    // === THÊM HÀM REGISTER VÀO ĐÂY ===
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        var result = await _authService.RegisterAsync(registerDto);
        
        if (!result)
        {
            return BadRequest(new { message = "Tài khoản đã tồn tại hoặc đăng ký thất bại rồi Khang ơi!" });
        }

        return Ok(new { message = "Đăng ký tài khoản và cấp 100 Gold thành công rực rỡ!" });
    }
}