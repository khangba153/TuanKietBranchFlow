using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.Users;
using TuanKietBranchFlow.Application.Services;

namespace TuanKietBranchFlow.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    // Nhận UserService từ DI
    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    // Lấy hồ sơ người dùng đang đăng nhập
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDTO>> GetCurrentProfileAsync()
    {
        // Lấy UserId từ JWT
        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        bool isValidUserId = int.TryParse(userIdValue, out int userId);

        if (!isValidUserId)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Token không hợp lệ",
                detail: "Token không chứa mã người dùng hợp lệ.");
        }

        UserProfileDTO? profile = await _userService.GetCurrentProfileAsync(userId);

        // Không tìm thấy tài khoản hợp lệ trong db
        if (profile == null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy hồ sơ",
                detail: "Không tìm thấy hồ sơ người dùng hiện tại.");
        }

        return Ok(profile);
    }
    
}