using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.Auth;
using TuanKietBranchFlow.Application.Services;

namespace TuanKietBranchFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    // Nhận AuthService từ DI
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // Nhận thông tin đăng nhập và trả JWT nếu hợp lệ
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDTO>> LoginAsync(
        [FromBody] LoginRequestDTO request)
    {
        LoginResponseDTO? response = await _authService.LoginAsync(request);

        // Trả cùng 1 thông báo để không tiết lộ tài khoản có tồn tại hay ko
        if (response == null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Đăng nhập thất bại",
                detail: "Tên đăng nhập hoặc mật khẩu không đúng");
        }

        return Ok(response);

    }
}