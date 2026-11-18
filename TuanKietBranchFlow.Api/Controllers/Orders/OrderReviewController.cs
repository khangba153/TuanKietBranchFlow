using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.Orders.Management;
using TuanKietBranchFlow.Application.Services.Orders;

namespace TuanKietBranchFlow.Api.Controllers.Orders;

[ApiController]
[Route("api/order-management/{orderId:int:min(1)}/review")]
[Authorize(Roles = "ADMIN")]
public class OrderReviewController : ControllerBase
{
    private readonly IOrderReviewService _orderReviewService;
    public OrderReviewController(IOrderReviewService service)
    {
        _orderReviewService = service;
    }

    /// <summary>Giữ nguyên hoặc hủy đúng lần báo sai đang xem; OWNER không được ghi.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderReviewResultDTO), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<ActionResult<OrderReviewResultDTO>> ResolveAsync(
        int orderId,
        ResolveOrderReviewRequestDTO request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId) || userId <= 0)
        {
            return Unauthorized();
        }

        OrderReviewResultDTO result = await _orderReviewService.ResolveAsync(userId, User.FindFirstValue(ClaimTypes.Role) ?? "", orderId, request);
        if (result.IsForbidden)
        {
            return Forbid();
        }

        if (result.IsInvalid)
        {
            return Problem(statusCode: 400, title: "Thao tác hoặc lý do không hợp lệ");
        }

        if (result.IsNotFound)
        {
            return Problem(statusCode: 404, title: "Không tìm thấy đơn trong phạm vi truy cập");
        }

        if (result.IsConflict)
        {
            return Problem(
                statusCode: 409,
                title: "Đơn hoặc quyền truy cập đã thay đổi. Hãy tải lại trước khi xử lý.");
        }

        return Ok(result);
    }
}
