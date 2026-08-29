using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.Employees;
using TuanKietBranchFlow.Application.Services;

namespace TuanKietBranchFlow.Api.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize(Roles = "OWNER,ADMIN")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    // Nhận IEmployeeService từ Di
    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    /// <summary>
    /// Lấy danh sách nhân viên theo chi nhánh
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<EmployeeListItemDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<EmployeeListItemDTO>>>
        GetEmployeesByBranchAsync(
            [FromQuery] int branchId,
            [FromQuery] string keyword = "",
            [FromQuery] bool? isActive = null)
    {
        // BranchId phải là 1 số nguyên dương
        if (branchId <= 0)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Chi nhánh không hợp lệ",
                detail: "BranchId phải lớn hơn 0.");
        }

        // Đọc danh tính người dùng hiện tại từ JWT
        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        string? role = User.FindFirstValue(ClaimTypes.Role);
        bool isValidUserId = int.TryParse(userIdValue, out int currentUserId);

        if (!isValidUserId || string.IsNullOrWhiteSpace(role))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Token không hợp lệ",
                detail: "Token không chứa đầy đủ thông tin người dùng.");
        }

        // Service kiểm tra chi nhánh, quyền truy cập và lấy nhân viên
        EmployeeListResultDTO result =
            await _employeeService.GetEmployeesByBranchAsync(
                currentUserId,
                role,
                branchId,
                keyword,
                isActive);

        // Chi nhánh không tồn tại hoặc bị xóa
        if (!result.IsBranchFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy chi nhánh",
                detail: "Chi nhánh được yêu cầu không tồn tại");
        }

        // Chi nhánh tồn tại nhưng không có quyền truy cập
        if (!result.HasAccess)
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Không có quyền truy cập",
                detail: "Bạn không được phân công tại chi nhánh này.");
        }

        return Ok(result.Employees);
    }
}   