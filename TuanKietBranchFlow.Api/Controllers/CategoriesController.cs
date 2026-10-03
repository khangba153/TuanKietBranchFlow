using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.MenuManagement;
using TuanKietBranchFlow.Application.Services;

namespace TuanKietBranchFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = "OWNER,ADMIN")]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Lấy danh mục chưa xóa cho ADMIN quản lý và OWNER xem.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MenuCategoryDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<MenuCategoryDTO>>> GetAllCategoriesAsync()
    {
        List<MenuCategoryDTO> categories =
            await _categoryService.GetAllCategoriesAsync();

        return Ok(categories);
    }

    /// <summary>
    /// Tạo danh mục mới. Chỉ ADMIN được phép thực hiện.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(MenuCategoryDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MenuCategoryDTO>> CreateCategoryAsync(
        [FromBody] CreateCategoryRequestDTO request)
    {
        CreateCategoryResultDTO result =
            await _categoryService.CreateCategoryAsync(request);

        if (result.IsNameInvalid)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Tên danh mục không hợp lệ",
                detail: "Tên danh mục phải có nội dung và không vượt quá 150 ký tự.");
        }

        if (result.IsNameDuplicated)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Danh mục đã tồn tại",
                detail: "Tên danh mục đã được sử dụng.");
        }

        // Bảo vệ trường hợp kết quả Service không nhất quán
        if (result.Category == null)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể tạo danh mục",
                detail: "Hệ thống không nhận được dữ liệu danh mục vừa tạo.");
        }

        // Chưa có route chi tiết danh mục để tạo Location đến đó
        return StatusCode(StatusCodes.Status201Created, result.Category);
    }

    /// <summary>
    /// Cập nhật tên và trạng thái hoạt động của danh mục. Chỉ ADMIN được phép thực hiện.
    /// </summary>
    [HttpPut("{categoryId:int}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(MenuCategoryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MenuCategoryDTO>> UpdateCategoryAsync(
        int categoryId,
        [FromBody] UpdateCategoryRequestDTO request)
    {
        UpdateCategoryResultDTO result =
            await _categoryService.UpdateCategoryAsync(categoryId, request);

        if (result.IsRequestInvalid)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Yêu cầu cập nhật không hợp lệ",
                detail: "Tên danh mục phải có nội dung, không vượt quá 150 ký tự và trạng thái hoạt động phải được cung cấp.");
        }

        if (result.IsCategoryNotFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy danh mục",
                detail: "Danh mục không tồn tại hoặc đã bị xóa.");
        }

        if (result.IsNameDuplicated)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Tên danh mục đã tồn tại",
                detail: "Tên danh mục đã được sử dụng bởi một danh mục khác.");
        }

        // Bảo vệ trường hợp kết quả Service không nhất quán
        if (result.Category == null)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể cập nhật danh mục",
                detail: "Hệ thống không nhận được dữ liệu danh mục sau khi cập nhật.");
        }

        return Ok(result.Category);
    }

    /// <summary>
    /// Xóa mềm danh mục. Chỉ ADMIN được phép thực hiện.
    /// </summary>
    [HttpDelete("{categoryId:int}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteCategoryAsync(int categoryId)
    {
        DeleteCategoryResultDTO result =
            await _categoryService.DeleteCategoryAsync(categoryId);

        if (result.IsCategoryNotFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy danh mục",
                detail: "Danh mục không tồn tại hoặc đã bị xóa.");
        }

        // Bảo vệ trường hợp kết quả Service không nhất quán
        if (!result.IsDeleted)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể xóa danh mục",
                detail: "Hệ thống không xác nhận được kết quả xóa danh mục.");
        }

        return NoContent();
    }
}