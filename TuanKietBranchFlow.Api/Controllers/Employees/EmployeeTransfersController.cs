using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.Employees.Transfers;
using TuanKietBranchFlow.Application.Services.Employees;

namespace TuanKietBranchFlow.Api.Controllers.Employees;

[ApiController]
[Route("api/employee-transfers")]
[Authorize(Roles = "ADMIN,OWNER")]
public class EmployeeTransfersController : ControllerBase
{
    private readonly IEmployeeTransferService _employeeTransferService;
    public EmployeeTransfersController(IEmployeeTransferService transferService)
    {
        _employeeTransferService = transferService;
    }

    [HttpGet("targets")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetTargetsAsync([FromQuery] int sourceBranchId)
    {
        if (sourceBranchId <= 0)
        {
            return Problem(statusCode: 400, detail: "Chi nhánh nguồn không hợp lệ.");
        }

        List<EmployeeTransferTargetDTO>? targets = await _employeeTransferService.GetTargetsAsync(UserId, sourceBranchId);
        if (targets == null)
        {
            return Problem(statusCode: 403, detail: "Bạn không quản lý chi nhánh nguồn.");
        }

        return Ok(targets);
    }

    // Trả tối đa 21 mục: Web dùng mục thứ 21 để biết còn trang sau, mỗi trang 20 mục.
    [HttpGet]
    public async Task<IActionResult> GetRequestsAsync(
        [FromQuery] bool incoming = false,
        [FromQuery] int page = 1)
    {
        if (page < 1 || page > 10000)
        {
            return Problem(statusCode: 400, detail: "Trang không hợp lệ.");
        }

        return Ok(await _employeeTransferService.GetRequestsAsync(UserId, User.IsInRole("OWNER"), incoming, page));
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> CreateAsync(EmployeeTransferCreateDTO input)
    {
        return ToResponse(await _employeeTransferService.CreateAsync(UserId, input));
    }

    [HttpPost("{requestId:int}/{decision:regex(^(accept|reject|cancel)$)}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DecideAsync(
        int requestId,
        string decision,
        EmployeeTransferDecisionDTO input)
    {
        return ToResponse(await _employeeTransferService.DecideAsync(UserId, requestId, decision, input.Reason));
    }

    private int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId) ? userId : 0;

    private IActionResult ToResponse(EmployeeTransferResultDTO result)
    {
        if (result.Request != null)
        {
            return Ok(result.Request);
        }

        int statusCode = result.ErrorCode switch
        {
            "Forbidden" => 403,
            "NotFound" => 404,
            "Conflict" => 409,
            _ => 400
        };
        return Problem(
            statusCode: statusCode,
            title: "Không thể xử lý chuyển chi nhánh",
            detail: result.ErrorMessage);
    }
}
