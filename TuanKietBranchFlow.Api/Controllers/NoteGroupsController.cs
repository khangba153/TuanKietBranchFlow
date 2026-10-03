using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.MenuManagement;
using TuanKietBranchFlow.Application.Services;

namespace TuanKietBranchFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = "OWNER,ADMIN")]
[Route("api/note-groups")]
public class NoteGroupsController : ControllerBase
{
    private readonly INoteGroupService _noteGroupService;

    public NoteGroupsController(
        INoteGroupService noteGroupService)
    {
        _noteGroupService = noteGroupService;
    }

    /// <summary>
    /// Lấy các nhóm ghi chú và lựa chọn ghi chú chưa xóa cho ADMIN quản lý, OWNER xem.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MenuNoteGroupDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<MenuNoteGroupDTO>>> GetAllNoteGroupsAsync()
    {
        List<MenuNoteGroupDTO> noteGroups =
            await _noteGroupService.GetAllNoteGroupsAsync();

        return Ok(noteGroups);
    }

    /// <summary>
    /// Tạo nhóm ghi chú mới. Chỉ ADMIN được phép thực hiện.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(MenuNoteGroupDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MenuNoteGroupDTO>> CreateNoteGroupAsync(
        [FromBody] CreateNoteGroupRequestDTO request)
    {
        CreateNoteGroupResultDTO result =
            await _noteGroupService.CreateNoteGroupAsync(request);

        if (result.IsNameInvalid)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Tên nhóm ghi chú không hợp lệ",
                detail: "Tên nhóm ghi chú phải có nội dung và không vượt quá 100 ký tự.");
        }

        if (result.IsNameDuplicated)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Nhóm ghi chú đã tồn tại",
                detail: "Tên nhóm ghi chú đã được sử dụng.");
        }

        // Bảo vệ trường hợp kết quả Service không nhất quán
        if (result.NoteGroup == null)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể tạo nhóm ghi chú",
                detail: "Hệ thống không nhận được dữ liệu nhóm ghi chú vừa tạo.");
        }

        // Chưa có route chi tiết nhóm ghi chú để tạo Location đến đó
        return StatusCode(StatusCodes.Status201Created, result.NoteGroup);
    }

    /// <summary>
    /// Cập nhật nhóm ghi chú. Chỉ ADMIN được phép thực hiện.
    /// </summary>
    [HttpPut("{noteGroupId:int}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(MenuNoteGroupDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MenuNoteGroupDTO>> UpdateNoteGroupAsync(
        int noteGroupId,
        [FromBody] UpdateNoteGroupRequestDTO request)
    {
        UpdateNoteGroupResultDTO result =
            await _noteGroupService.UpdateNoteGroupAsync(
                noteGroupId,
                request);

        if (result.IsRequestInvalid)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Yêu cầu cập nhật không hợp lệ",
                detail: "Tên nhóm ghi chú phải có nội dung, không vượt quá 100 ký tự và trạng thái hoạt động phải được cung cấp.");
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
                title: "Tên nhóm ghi chú đã tồn tại",
                detail: "Tên nhóm ghi chú đã được sử dụng bởi một nhóm khác.");
        }

        // Bảo vệ trường hợp kết quả Service không nhất quán
        if (result.NoteGroup == null)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể cập nhật nhóm ghi chú",
                detail: "Hệ thống không nhận được dữ liệu nhóm ghi chú sau khi cập nhật.");
        }

        return Ok(result.NoteGroup);
    }

    /// <summary>
    /// Xóa mềm nhóm ghi chú, giữ lựa chọn ghi chú con. Chỉ ADMIN được phép thực hiện.
    /// </summary>
    [HttpDelete("{noteGroupId:int}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteNoteGroupAsync(int noteGroupId)
    {
        DeleteNoteGroupResultDTO result =
            await _noteGroupService.DeleteNoteGroupAsync(noteGroupId);

        if (result.IsNoteGroupNotFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy nhóm ghi chú",
                detail: "Nhóm ghi chú không tồn tại hoặc đã bị xóa.");
        }

        // Bảo vệ trường hợp kết quả Service không nhất quán
        if (!result.IsDeleted)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể xóa nhóm ghi chú",
                detail: "Hệ thống không xác nhận được kết quả xóa nhóm ghi chú.");
        }

        return NoContent();
    }
}
