using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.MenuManagement;
using TuanKietBranchFlow.Application.Services;

namespace TuanKietBranchFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = "OWNER,ADMIN")]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Lấy các món chưa xóa cho ADMIN quản lý và OWNER xem.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MenuProductDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<MenuProductDTO>>> GetAllProductsAsync()
    {
        List<MenuProductDTO> products =
            await _productService.GetAllProductsAsync();

        return Ok(products);
    }

    /// <summary>
    /// Tạo Product cùng các lựa chọn size và giá. Chỉ ADMIN được phép thực hiện.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(MenuProductDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MenuProductDTO>> CreateProductAsync(
        [FromBody] CreateProductRequestDTO request)
    {
        CreateProductResultDTO result =
            await _productService.CreateProductAsync(request);

        if (result.IsRequestInvalid)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Thông tin sản phẩm không hợp lệ",
                detail: "Cần có danh mục, tên sản phẩm hợp lệ và ít nhất hai size.");
        }

        if (result.IsProductSizesInvalid)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Thông tin size và giá không hợp lệ",
                detail: "Các size phải khác nhau, đang hoạt động, chưa bị xóa và có giá lớn hơn 0.");
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
                title: "Sản phẩm đã tồn tại",
                detail: "Tên sản phẩm đã được sử dụng.");
        }

        // Bảo vệ trường hợp kết quả Service không nhất quán
        if (result.Product == null)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể tạo sản phẩm",
                detail: "Hệ thống không nhận được dữ liệu sản phẩm vừa tạo.");
        }

        // Chưa có route chi tiết Product để tạo Location đến đó
        return StatusCode(StatusCodes.Status201Created, result.Product);
    }

    /// <summary>
    /// Cập nhật Product cùng các lựa chọn size và giá. Chỉ ADMIN được phép thực hiện.
    /// </summary>
    [HttpPut("{productId:int}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(MenuProductDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MenuProductDTO>> UpdateProductAsync(
        int productId,
        [FromBody] UpdateProductRequestDTO request)
    {
        UpdateProductResultDTO result =
            await _productService.UpdateProductAsync(productId, request);

        if (result.IsRequestInvalid)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Yêu cầu cập nhật không hợp lệ",
                detail: "Cần có ProductId, danh mục, tên, trạng thái và ít nhất hai size hợp lệ.");
        }

        if (result.IsProductSizesInvalid)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Thông tin size và giá không hợp lệ",
                detail: "Size phải khác nhau; size mới phải đang hoạt động và giá phải lớn hơn 0.");
        }

        if (result.IsProductNotFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy sản phẩm",
                detail: "Sản phẩm không tồn tại hoặc đã bị xóa.");
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
                title: "Tên sản phẩm đã tồn tại",
                detail: "Tên sản phẩm đã được sử dụng bởi một sản phẩm khác.");
        }

        // Bảo vệ trường hợp Service không trả Product sau khi cập nhật
        if (result.Product == null)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể cập nhật sản phẩm",
                detail: "Hệ thống không nhận được dữ liệu sản phẩm sau khi cập nhật.");
        }

        return Ok(result.Product);
    }

    /// <summary>
    /// Xóa mềm Product. Chỉ ADMIN được phép thực hiện.
    /// </summary>
    [HttpDelete("{productId:int}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteProductAsync(int productId)
    {
        DeleteProductResultDTO result =
            await _productService.DeleteProductAsync(productId);

        if (result.IsProductNotFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy sản phẩm",
                detail: "Sản phẩm không tồn tại hoặc đã bị xóa.");
        }

        // Bảo vệ trường hợp Service không xác nhận đã xóa
        if (!result.IsDeleted)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Không thể xóa sản phẩm",
                detail: "Hệ thống không xác nhận được kết quả xóa sản phẩm.");
        }

        return NoContent();
    }
}