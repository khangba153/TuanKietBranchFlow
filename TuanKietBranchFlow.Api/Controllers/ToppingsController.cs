using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.MenuManagement;
using TuanKietBranchFlow.Application.Services;

namespace TuanKietBranchFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = "ADMIN")]
[Route("api/toppings")]
public class ToppingsController : ControllerBase
{
    private readonly IToppingService _toppingService;

    public ToppingsController(IToppingService toppingService)
    {
        _toppingService = toppingService;
    }

    /// <summary>
    /// Tạo topping trong nhóm đã chọn. Chỉ ADMIN được phép thực hiện.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MenuToppingDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MenuToppingDTO>> CreateToppingAsync(
        [FromBody] CreateToppingRequestDTO request)
    {
        CreateToppingResultDTO result =
            await _toppingService.CreateToppingAsync(request);

        if (result.IsRequestInvalid)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Yêu cầu tạo topping không hợp lệ",
                detail: "Nhóm topping phải hợp lệ; tên phải có nội dung và không vượt quá 150 ký tự; giá phải lớn hơn 0, không vượt quá giới hạn database và có tối đa 2 chữ số thập phân.");
        }

        if (result.IsToppingGroupNotFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy nhóm topping",
                detail: "Nhóm topping không tồn tại hoặc đã bị xóa.");
        }

        if (result.IsNameDuplicated)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Tên topping đã tồn tại",
                detail: "Tên topping đã được sử dụng.");
        }

        // Bảo vệ trường hợp kết quả Service không nhất quán
        if (result.Topping == null)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể tạo topping",
                detail: "Hệ thống không nhận được dữ liệu topping vừa tạo.");
        }

        // Chưa có route chi tiết topping để tạo Location đến đó
        return StatusCode(StatusCodes.Status201Created, result.Topping);
    }

    /// <summary>
    /// Cập nhật tên, giá và trạng thái topping. Chỉ ADMIN được phép thực hiện.
    /// </summary>
    [HttpPut("{toppingId:int}")]
    [ProducesResponseType(typeof(MenuToppingDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MenuToppingDTO>> UpdateToppingAsync(
        int toppingId,
        [FromBody] UpdateToppingRequestDTO request)
    {
        UpdateToppingResultDTO result =
            await _toppingService.UpdateToppingAsync(toppingId, request);

        if (result.IsRequestInvalid)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Yêu cầu cập nhật topping không hợp lệ",
                detail: "Tên phải có nội dung và không vượt quá 150 ký tự; giá phải lớn hơn 0, trong giới hạn database và có tối đa 2 chữ số thập phân; trạng thái hoạt động phải được cung cấp.");
        }

        if (result.IsToppingNotFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy topping",
                detail: "Topping không tồn tại, đã bị xóa hoặc thuộc nhóm đã bị xóa.");
        }

        if (result.IsNameDuplicated)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Tên topping đã tồn tại",
                detail: "Tên topping đã được sử dụng bởi một topping khác.");
        }

        // Bảo vệ trường hợp kết quả Service không nhất quán
        if (result.Topping == null)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể cập nhật topping",
                detail: "Hệ thống không nhận được dữ liệu topping sau khi cập nhật.");
        }

        return Ok(result.Topping);
    }

    /// <summary>
    /// Xóa mềm topping, giữ lịch sử đơn hàng. Chỉ ADMIN được phép thực hiện.
    /// </summary>
    [HttpDelete("{toppingId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteToppingAsync(int toppingId)
    {
        DeleteToppingResultDTO result =
            await _toppingService.DeleteToppingAsync(toppingId);

        if (result.IsToppingNotFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy topping",
                detail: "Topping không tồn tại, đã bị xóa hoặc thuộc nhóm đã bị xóa.");
        }

        // Bảo vệ trường hợp kết quả Service không nhất quán
        if (!result.IsDeleted)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể xóa topping",
                detail: "Hệ thống không xác nhận được kết quả xóa topping.");
        }

        return NoContent();
    }
}
