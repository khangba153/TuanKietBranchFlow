using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.OrderMenus;
using TuanKietBranchFlow.Application.Services;

namespace TuanKietBranchFlow.Api.Controllers;

[ApiController]
[Route("api/order-menu")]
[Authorize(Roles = "EMPLOYEE")]
public class OrderMenuController : ControllerBase
{
    private readonly IOrderMenuService _orderMenuService;

    public OrderMenuController(IOrderMenuService orderMenuService)
    {
        _orderMenuService = orderMenuService;
    }

    /// <summary>
    /// Lấy menu gọi món đang bán tại chi nhánh của employee
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(OrderMenuResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderMenuResponseDTO>> GetOrderMenuAsync(
        [FromQuery] int branchId)
    {
        // Kiểm tra id chi nhánh phải phải là số nguyên dương
        if (branchId <= 0)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Chi nhánh không hợp lệ",
                detail: "BranchId phải lớn hơn 0.");
        }

        // Đọc id empployee hiện tại từ JWT
        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        bool isValidUserId = int.TryParse(userIdValue, out int currentUserId);

        if (!isValidUserId)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Token không hợp lệ",
                detail: "Token không chứa mã người dùng hợp lệ.");
        }

        // Service kiểm tra chi nhánh, phân công và lấy menu
        OrderMenuResultDTO result =
            await _orderMenuService.GetOrderMenuAsync(currentUserId, branchId);

        if (!result.IsBranchFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy chi nhánh",
                detail: "Chi nhánh được yêu cầu không tồn tại.");
        }

        if (!result.HasAccess)
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Không có quyền truy cập",
                detail: "Bạn không được phân công tại chi nhánh này.");
        }

        return Ok(result.Menu);
    }
}