using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.Orders.Management;
using TuanKietBranchFlow.Application.Services.Orders;

namespace TuanKietBranchFlow.Api.Controllers.Orders;

[ApiController]
[Route("api/order-management")]
[Authorize(Roles = "ADMIN,OWNER")]
public class OrderManagementController : ControllerBase
{
    private readonly IOrderManagementService _orderManagementService;
    public OrderManagementController(IOrderManagementService service)
    {
        _orderManagementService = service;
    }

    /// <summary>Danh sách đơn theo chi nhánh được phép, bộ lọc và phân trang.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(OrderManagementPageDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderManagementPageDTO>> GetOrdersAsync([FromQuery] OrderManagementQueryDTO query)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId) || userId <= 0)
        {
            return Unauthorized();
        }

        string role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        OrderManagementListResultDTO result = await _orderManagementService.GetOrdersAsync(userId, role, query);
        if (result.IsForbidden)
        {
            return Forbid();
        }

        if (!result.IsValid)
        {
            return Problem(statusCode: 400, title: "Bộ lọc không hợp lệ", detail: result.ErrorMessage);
        }

        if (!result.IsBranchFound)
        {
            return Problem(statusCode: 404, title: "Không tìm thấy chi nhánh");
        }

        return Ok(result.Data);
    }

    /// <summary>Chi tiết đơn trong phạm vi; không tồn tại hoặc ngoài quyền trả 404.</summary>
    [HttpGet("{orderId:int:min(1)}")]
    [ProducesResponseType(typeof(OrderManagementDetailDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderManagementDetailDTO>> GetDetailAsync(int orderId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId) || userId <= 0)
        {
            return Unauthorized();
        }

        string role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        OrderManagementDetailDTO? detail = await _orderManagementService.GetDetailAsync(userId, role, orderId);
        if (detail == null)
        {
            return Problem(statusCode: 404, title: "Không tìm thấy đơn trong phạm vi truy cập");
        }

        return Ok(detail);
    }
}
