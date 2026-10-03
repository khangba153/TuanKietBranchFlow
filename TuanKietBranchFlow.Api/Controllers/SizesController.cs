using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.MenuManagement;
using TuanKietBranchFlow.Application.Services;

namespace TuanKietBranchFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = "OWNER,ADMIN")]
[Route("api/sizes")]
public class SizesController : ControllerBase
{
    private readonly ISizeService _sizeService;

    public SizesController(ISizeService sizeService)
    {
        _sizeService = sizeService;
    }

    /// <summary>
    /// Lấy các size chưa xóa cho ADMIN quản lý và OWNER xem.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MenuSizeDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<MenuSizeDTO>>> GetAllSizesAsync()
    {
        List<MenuSizeDTO> sizes =
            await _sizeService.GetAllSizesAsync();

        return Ok(sizes);
    }

    /// <summary>
    /// Tạo Size mới. Chỉ ADMIN được phép thực hiện.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(MenuSizeDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MenuSizeDTO>> CreateSizeAsync(
        [FromBody] CreateSizeRequestDTO request)
    {
        CreateSizeResultDTO result =
            await _sizeService.CreateSizeAsync(request);

        if (result.IsNameInvalid)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Tên size không hợp lệ",
                detail: "Tên size phải có nội dung và không vượt quá 50 ký tự.");
        }

        if (result.IsNameDuplicated)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Size đã tồn tại",
                detail: "Tên size đã được sử dụng.");
        }

        // Bảo vệ trường hợp kết quả Service không nhất quán
        if (result.Size == null)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể tạo size",
                detail: "Hệ thống không nhận được dữ liệu size vừa tạo.");
        }

        // Chưa có route chi tiết Size để tạo Location đến đó
        return StatusCode(StatusCodes.Status201Created, result.Size);
    }

    /// <summary>
    /// Cập nhật tên và trạng thái hoạt động của Size. Chỉ ADMIN được phép thực hiện.
    /// </summary>
    [HttpPut("{sizeId:int}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(MenuSizeDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MenuSizeDTO>> UpdateSizeAsync(
        int sizeId,
        [FromBody] UpdateSizeRequestDTO request)
    {
        UpdateSizeResultDTO result =
            await _sizeService.UpdateSizeAsync(sizeId, request);

        if (result.IsRequestInvalid)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Yêu cầu cập nhật không hợp lệ",
                detail: "Tên size phải có nội dung, không vượt quá 50 ký tự và trạng thái hoạt động phải được cung cấp.");
        }

        if (result.IsSizeNotFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy size",
                detail: "Size không tồn tại hoặc đã bị xóa.");
        }

        if (result.IsNameDuplicated)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Tên size đã tồn tại",
                detail: "Tên size đã được sử dụng bởi một size khác.");
        }

        // Bảo vệ trường hợp kết quả Service không nhất quán
        if (result.Size == null)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể cập nhật size",
                detail: "Hệ thống không nhận được dữ liệu size sau khi cập nhật.");
        }

        return Ok(result.Size);
    }
    /// <summary>
    /// Xóa mềm Size. Chỉ ADMIN được phép thực hiện.
    /// </summary>
    [HttpDelete("{sizeId:int}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteSizeAsync(int sizeId)
    {
        DeleteSizeResultDTO result =
            await _sizeService.DeleteSizeAsync(sizeId);

        if (result.IsSizeNotFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy size",
                detail: "Size không tồn tại hoặc đã bị xóa.");
        }

        // Bảo vệ trường hợp kết quả Service không nhất quán
        if (!result.IsDeleted)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể xóa size",
                detail: "Hệ thống không xác nhận được kết quả xóa size.");
        }

        return NoContent();
    }
}