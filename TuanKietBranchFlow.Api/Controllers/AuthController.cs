using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.Auth;
using TuanKietBranchFlow.Application.Services;
using System.Security.Claims;

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

    // Đọc thông tin người dùng từ JWT
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentUserDTO> GetCurrentUser()
    {
        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        string? userName = User.FindFirstValue(ClaimTypes.Name);
        string? role = User.FindFirstValue(ClaimTypes.Role);

        // Token hợp lệ phải có đủ 3 claim
        bool isValidUserId = int.TryParse(userIdValue, out int userId);

        if (!isValidUserId
            || string.IsNullOrWhiteSpace(userName)
            || string.IsNullOrWhiteSpace(role))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Token không hợp lệ",
                detail: "Token không chứa đầy đủ thông tin người dùng.");
        }

        CurrentUserDTO response = new CurrentUserDTO
        {
            UserId = userId,
            Username = userName,
            Role = role
        };

        return Ok(response);
    }

}
