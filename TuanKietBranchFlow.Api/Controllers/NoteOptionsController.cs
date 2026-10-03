using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.MenuManagement;
using TuanKietBranchFlow.Application.Services;

namespace TuanKietBranchFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = "ADMIN")]
[Route("api/note-options")]
public class NoteOptionsController : ControllerBase
{
    private readonly INoteOptionService _noteOptionService;

    public NoteOptionsController(INoteOptionService noteOptionService)
    {
        _noteOptionService = noteOptionService;
    }

    /// <summary>
    /// Tạo lựa chọn ghi chú trong nhóm đã chọn. Chỉ ADMIN được phép thực hiện.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MenuNoteOptionDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MenuNoteOptionDTO>> CreateNoteOptionAsync(
        [FromBody] CreateNoteOptionRequestDTO request)
    {
        CreateNoteOptionResultDTO result =
            await _noteOptionService.CreateNoteOptionAsync(request);

        if (result.IsRequestInvalid)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Yêu cầu tạo lựa chọn ghi chú không hợp lệ",
                detail: "Nhóm ghi chú phải hợp lệ; tên phải có nội dung và không vượt quá 100 ký tự.");
        }

        if (result.IsNoteGroupNotFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy nhóm ghi chú",
                detail: "Nhóm ghi chú không tồn tại hoặc đã bị xóa.");
        }

        if (result.IsNameDuplicated)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Tên lựa chọn ghi chú đã tồn tại",
                detail: "Tên lựa chọn ghi chú đã được sử dụng trong nhóm này.");
        }

        // Bảo vệ trường hợp kết quả Service không nhất quán
        if (result.NoteOption == null)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể tạo lựa chọn ghi chú",
                detail: "Hệ thống không nhận được dữ liệu lựa chọn ghi chú vừa tạo.");
        }

        // Chưa có route chi tiết lựa chọn ghi chú để tạo Location đến đó
        return StatusCode(StatusCodes.Status201Created, result.NoteOption);
    }

    /// <summary>
    /// Cập nhật tên và trạng thái lựa chọn ghi chú. Chỉ ADMIN được phép thực hiện.
    /// </summary>
    [HttpPut("{noteOptionId:int}")]
    [ProducesResponseType(typeof(MenuNoteOptionDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MenuNoteOptionDTO>> UpdateNoteOptionAsync(
        int noteOptionId,
        [FromBody] UpdateNoteOptionRequestDTO request)
    {
        UpdateNoteOptionResultDTO result =
            await _noteOptionService.UpdateNoteOptionAsync(noteOptionId, request);

        if (result.IsRequestInvalid)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Yêu cầu cập nhật lựa chọn ghi chú không hợp lệ",
                detail: "Tên phải có nội dung, không vượt quá 100 ký tự và trạng thái hoạt động phải được cung cấp.");
        }

        if (result.IsNoteOptionNotFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy lựa chọn ghi chú",
                detail: "Lựa chọn ghi chú không tồn tại, đã bị xóa hoặc thuộc nhóm đã bị xóa.");
        }

        if (result.IsNameDuplicated)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Tên lựa chọn ghi chú đã tồn tại",
                detail: "Tên lựa chọn ghi chú đã được sử dụng bởi một lựa chọn khác trong nhóm này.");
        }

        // Bảo vệ trường hợp kết quả Service không nhất quán
        if (result.NoteOption == null)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể cập nhật lựa chọn ghi chú",
                detail: "Hệ thống không nhận được dữ liệu lựa chọn ghi chú sau khi cập nhật.");
        }

        return Ok(result.NoteOption);
    }

    /// <summary>
    /// Xóa mềm lựa chọn ghi chú, giữ lịch sử đơn hàng. Chỉ ADMIN được phép thực hiện.
    /// </summary>
    [HttpDelete("{noteOptionId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteNoteOptionAsync(int noteOptionId)
    {
        DeleteNoteOptionResultDTO result =
            await _noteOptionService.DeleteNoteOptionAsync(noteOptionId);

        if (result.IsNoteOptionNotFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy lựa chọn ghi chú",
                detail: "Lựa chọn ghi chú không tồn tại, đã bị xóa hoặc thuộc nhóm đã bị xóa.");
        }

        // Bảo vệ trường hợp kết quả Service không nhất quán
        if (!result.IsDeleted)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể xóa lựa chọn ghi chú",
                detail: "Hệ thống không xác nhận được kết quả xóa lựa chọn ghi chú.");
        }

        return NoContent();
    }
}
