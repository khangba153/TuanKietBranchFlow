using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.Branches;
using TuanKietBranchFlow.Application.Services;

namespace TuanKietBranchFlow.Api.Controllers;

[ApiController]
[Route("api/branches")]
[Authorize(Roles = "OWNER,ADMIN,EMPLOYEE")]
public class BranchesController : ControllerBase
{
    private readonly IBranchService _branchService;

    public BranchesController(IBranchService branchService)
    {
        _branchService = branchService;
    }

    // Lấy danh sách chi nhánh người dùng hiện tại được phép truy cập
    [HttpGet("accessible")]
    [ProducesResponseType(typeof(List<AccessibleBranchDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<AccessibleBranchDTO>>> GetAccessibleBranchesAsync()
    {
        // Đọc UserId và Role từ JWT
        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        string? role = User.FindFirstValue(ClaimTypes.Role);
        bool isValidUserId = int.TryParse(userIdValue, out int userId);

        // Không tiếp tục nếu thiếu thông tin người dùng
        if (!isValidUserId || string.IsNullOrWhiteSpace(role))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Token không hợp lệ",
                detail: "Token không chứa đầy đủ thông tin người dùng.");
        }

        List<AccessibleBranchDTO> response = await _branchService.GetAccessibleBranchesAsync(userId, role);

        return Ok(response);
    }
}