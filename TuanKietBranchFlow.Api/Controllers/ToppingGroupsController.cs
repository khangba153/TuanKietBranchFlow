using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.MenuManagement;
using TuanKietBranchFlow.Application.Services;

namespace TuanKietBranchFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = "OWNER,ADMIN")]
[Route("api/topping-groups")]
public class ToppingGroupsController : ControllerBase
{
    private readonly IToppingGroupService _toppingGroupService;

    public ToppingGroupsController(
        IToppingGroupService toppingGroupService)
    {
        _toppingGroupService = toppingGroupService;
    }

    /// <summary>
    /// Lấy các nhóm topping và topping chưa xóa cho ADMIN quản lý, OWNER xem.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MenuToppingGroupDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<MenuToppingGroupDTO>>> GetAllToppingGroupsAsync()
    {
        List<MenuToppingGroupDTO> toppingGroups =
            await _toppingGroupService.GetAllToppingGroupsAsync();

        return Ok(toppingGroups);
    }

    /// <summary>
    /// Tạo nhóm topping mới. Chỉ ADMIN được phép thực hiện.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(MenuToppingGroupDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MenuToppingGroupDTO>> CreateToppingGroupAsync(
        [FromBody] CreateToppingGroupRequestDTO request)
    {
        CreateToppingGroupResultDTO result =
            await _toppingGroupService.CreateToppingGroupAsync(request);

        if (result.IsNameInvalid)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Tên nhóm topping không hợp lệ",
                detail: "Tên nhóm topping phải có nội dung và không vượt quá 100 ký tự.");
        }

        if (result.IsNameDuplicated)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Nhóm topping đã tồn tại",
                detail: "Tên nhóm topping đã được sử dụng.");
        }

        // Bảo vệ trường hợp kết quả Service không nhất quán
        if (result.ToppingGroup == null)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể tạo nhóm topping",
                detail: "Hệ thống không nhận được dữ liệu nhóm topping vừa tạo.");
        }

        // Chưa có route chi tiết nhóm topping để tạo Location đến đó
        return StatusCode(StatusCodes.Status201Created, result.ToppingGroup);
    }

    /// <summary>
    /// Cập nhật nhóm topping. Chỉ ADMIN được phép thực hiện.
    /// </summary>
    [HttpPut("{toppingGroupId:int}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(MenuToppingGroupDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MenuToppingGroupDTO>> UpdateToppingGroupAsync(
        int toppingGroupId,
        [FromBody] UpdateToppingGroupRequestDTO request)
    {
        UpdateToppingGroupResultDTO result =
            await _toppingGroupService.UpdateToppingGroupAsync(
                toppingGroupId,
                request);

        if (result.IsRequestInvalid)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Yêu cầu cập nhật không hợp lệ",
                detail: "Tên nhóm topping phải có nội dung, không vượt quá 100 ký tự và trạng thái hoạt động phải được cung cấp.");
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
                title: "Tên nhóm topping đã tồn tại",
                detail: "Tên nhóm topping đã được sử dụng bởi một nhóm khác.");
        }

        // Bảo vệ trường hợp kết quả Service không nhất quán
        if (result.ToppingGroup == null)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể cập nhật nhóm topping",
                detail: "Hệ thống không nhận được dữ liệu nhóm topping sau khi cập nhật.");
        }

        return Ok(result.ToppingGroup);
    }

    /// <summary>
    /// Xóa mềm nhóm topping, giữ topping con. Chỉ ADMIN được phép thực hiện.
    /// </summary>
    [HttpDelete("{toppingGroupId:int}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteToppingGroupAsync(int toppingGroupId)
    {
        DeleteToppingGroupResultDTO result =
            await _toppingGroupService.DeleteToppingGroupAsync(toppingGroupId);

        if (result.IsToppingGroupNotFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy nhóm topping",
                detail: "Nhóm topping không tồn tại hoặc đã bị xóa.");
        }

        // Bảo vệ trường hợp kết quả Service không nhất quán
        if (!result.IsDeleted)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể xóa nhóm topping",
                detail: "Hệ thống không xác nhận được kết quả xóa nhóm topping.");
        }

        return NoContent();
    }
}