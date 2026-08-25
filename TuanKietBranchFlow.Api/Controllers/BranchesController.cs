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

    // Lấy thông tin 1 chi nhánh nếu người dùng có quyền truy cập
    [HttpGet("{branchId:int}")]
    [ProducesResponseType(typeof(AccessibleBranchDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccessibleBranchDTO>> GetAccessibleBranchByIdAsync([FromRoute] int branchId)
    {
        // Đọc UserId và Role từ JWT đã được xác thực
        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        string? role = User.FindFirstValue(ClaimTypes.Role);
        bool isValidUserId = int.TryParse(userIdValue, out int userId);

        if (!isValidUserId || string.IsNullOrWhiteSpace(role))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Token không hợp lệ",
                detail: "Token không chứa đầy đủ thông tin người dùng.");
        }

        BranchAccessResultDTO result = await _branchService.GetAccessibleBranchByIdAsync(userId, role, branchId);

        // Branch không tồn tại hoặc đã bị xóa
        if (!result.IsFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy chi nhánh",
                detail: "Chi nhánh được yêu cầu không tồn tại.");
        }

        // Branch tồn tại nhưng người dùng không được phân công
        if (!result.HasAccess)
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Không có quyền truy cập",
                detail: "Bạn không được phân công tại chi nhánh này.");
        }

        return Ok(result.Branch);
    }
}