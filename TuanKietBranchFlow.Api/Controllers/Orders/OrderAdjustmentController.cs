using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.Orders.Management;
using TuanKietBranchFlow.Application.Services.Orders;

namespace TuanKietBranchFlow.Api.Controllers.Orders;

[ApiController, Authorize(Roles = "ADMIN")]
[Route("api/order-management/{orderId:int:min(1)}/adjustment")]
public class OrderAdjustmentController : ControllerBase
{
    private readonly IOrderAdjustmentService _orderAdjustmentService;

    public OrderAdjustmentController(IOrderAdjustmentService orderAdjustmentService)
    {
        _orderAdjustmentService = orderAdjustmentService;
    }

    /// <summary>Lựa chọn size/topping/ghi chú hiện tại cho các món đã có trong đơn.</summary>
    [HttpGet("options")]
    public async Task<ActionResult<OrderAdjustmentResultDTO>> GetOptionsAsync(int orderId)
    {
        if (!TryGetCurrentUserId(out int id))
        {
            return Unauthorized();
        }

        return Map(await _orderAdjustmentService.GetOptionsAsync(id, Role, orderId));
    }

    /// <summary>Tính lại bản xem trước, không ghi database.</summary>
    [HttpPost("preview")]
    public async Task<ActionResult<OrderAdjustmentResultDTO>> PreviewAsync(int orderId, AdjustOrderRequestDTO request)
    {
        if (!TryGetCurrentUserId(out int id))
        {
            return Unauthorized();
        }

        return Map(await _orderAdjustmentService.PreviewAsync(id, Role, orderId, request));
    }

    /// <summary>Xác nhận điều chỉnh, 409 khi đơn/quyền/giá khác bản xem trước.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderAdjustmentResultDTO), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<ActionResult<OrderAdjustmentResultDTO>> AdjustAsync(int orderId, AdjustOrderRequestDTO request)
    {
        if (!TryGetCurrentUserId(out int id))
        {
            return Unauthorized();
        }

        return Map(await _orderAdjustmentService.AdjustAsync(id, Role, orderId, request));
    }

    private string Role => User.FindFirstValue(ClaimTypes.Role) ?? "";

    private bool TryGetCurrentUserId(out int id) => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id) && id > 0;
    private ActionResult<OrderAdjustmentResultDTO> Map(OrderAdjustmentResultDTO result)
    {
        if (result.IsForbidden)
        {
            return Forbid();
        }

        if (result.IsNotFound)
        {
            return Problem(statusCode: 404, title: "Không tìm thấy đơn trong phạm vi truy cập");
        }

        if (result.IsConflict)
        {
            return Problem(statusCode: 409, title: result.Error ?? "Đơn hoặc quyền đã thay đổi. Hãy tải lại.");
        }

        if (result.Error != null)
        {
            return Problem(statusCode: 400, title: "Không thể điều chỉnh", detail: result.Error);
        }

        return Ok(result);
    }
}
