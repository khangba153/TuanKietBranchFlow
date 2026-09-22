using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.Orders;
using TuanKietBranchFlow.Application.Services;

namespace TuanKietBranchFlow.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize(Roles = "EMPLOYEE")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Tạo đơn hàng tại chi nhánh employee đang được phân công
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateOrderResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CreateOrderResponseDTO>> CreateOrderAsync(
        [FromBody] CreateOrderRequestDTO request)
    {
        // Lấy Id employee hiện tại từ JWT
        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        bool isValidUserId = int.TryParse(userIdValue, out int currentUserId);

        if (!isValidUserId)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Token không hợp lệ",
                detail: "Token không chứa mã người dùng hợp lệ.");
        }

        // Service kiểm tra dữ liệu, tính lại giá trị và lưu đơn
        CreateOrderResultDTO result =
            await _orderService.CreateOrderAsync(
                currentUserId, request);

        if (!result.IsBranchFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy chi nhánh",
                detail: result.ErrorMessage);
        }

        if (!result.HasAccess)
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Không có quyền tạo đơn",
                detail: result.ErrorMessage);
        }

        if (!result.IsOrderValid)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Đơn hàng không hợp lệ",
                detail: result.ErrorMessage);
        }

        // Bảo vệ trường hợp kết quả Service không nhất quán
        if (result.Order is null)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể tạo đơn hàng",
                detail: "Hệ thống không nhận được dữ liệu đơn vừa tạo.");
        }

        return CreatedAtAction(
            nameof(GetMyOrderDetailAsync),
            new { orderId = result.Order.Id },
            result.Order);
    }

    /// <summary>
    /// Lấy danh sách đơn do employee hiện tại tạo
    /// </summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(List<MyOrderListItemDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<MyOrderListItemDTO>>> GetMyOrdersAsync(
        [FromQuery] string keyword = "",
        [FromQuery] DateOnly? businessDate = null)
    {
        // Lấy Id employee hiện tại từ JWT, không nhận từ client
        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        bool isValidUser = int.TryParse(userIdValue, out int currentUserId);

        if (!isValidUser)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Token không hợp lệ",
                detail: "Token không chứa mã người dùng hợp lệ.");
        }

        List<MyOrderListItemDTO> orders =
            await _orderService.GetMyOrdersAsync(
                currentUserId,
                keyword,
                businessDate);

        return Ok(orders);
    }

    /// <summary>
    /// Lấy chi tiết một đơn do employee hiện tại tạo
    /// </summary>
    [HttpGet("{orderId:int}")]
    [ProducesResponseType(typeof(MyOrderDetailDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MyOrderDetailDTO>> GetMyOrderDetailAsync(
        int orderId)
    {
        if (orderId <= 0)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Mã đơn không hợp lệ",
                detail: "Mã đơn phải lớn hơn 0.");
        }

        // Lấy employee hiện tại từ JWT
        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        bool isValidUserId = int.TryParse(userIdValue, out int currentUserId);

        if (!isValidUserId)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Token không hợp lệ",
                detail: "Token không chứa mã người dùng hợp lệ.");
        }

        MyOrderDetailDTO? order =
            await _orderService.GetMyOrderDetailAsync(
                currentUserId,
                orderId);

        if (order is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy đơn hàng",
                detail: "Không tìm thấy đơn hàng thuộc quyền xem của bạn.");
        }

        return Ok(order);
    }

    /// <summary>
    /// Employee báo sai một đơn do chính mình tạo
    /// </summary>
    [HttpPatch("{orderId:int}/report")]
    [ProducesResponseType(typeof(ReportOrderResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ReportOrderResponseDTO>> ReportOrderAsync(
        int orderId,
        [FromBody] ReportOrderRequestDTO request)
    {
        if (orderId <= 0)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Mã đơn không hợp lệ",
                detail: "Mã đơn phải lớn hơn 0.");
        }

        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        bool isValidUserId = int.TryParse(userIdValue, out int currentUserId);

        if (!isValidUserId)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Token không hợp lệ",
                detail: "Token đăng nhập không hợp lệ.");
        }

        ReportOrderResult result =
            await _orderService.ReportOrderAsync(
                currentUserId,
                orderId,
                request);

        if (!result.IsReportValid)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Lý do báo sai không hợp lệ",
                detail: result.ErrorMessage);
        }

        if (!result.IsOrderFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy đơn hàng",
                detail: result.ErrorMessage);
        }

        if (!result.CanReport)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Không thể báo sai đơn hàng",
                detail: result.ErrorMessage);
        }

        // Bảo vệ trường hợp kết quả Service không nhất quán
        if (result.Order is null)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể báo sai đơn hàng",
                detail: "Hệ thống không nhận được dữ liệu đơn vừa cập nhật.");
        }

        return Ok(result.Order);
    }
}