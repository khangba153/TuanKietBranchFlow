using TuanKietBranchFlow.Application.DTOs.MenuManagement;

namespace TuanKietBranchFlow.Web.Models;

public class CategoryCreateApiResult
{
    // Cho biết request tạo danh mục có thành công hay không
    public bool IsSuccess { get; set; }

    // Giữ HTTP status code để giao diện xử lý khi cần
    public int StatusCode { get; set; }

    // Chứa danh mục vừa tạo khi API trả thành công
    public MenuCategoryDTO? Category { get; set; }

    // Chứa thông báo lỗi đọc từ ProblemDetails
    public string ErrorMessage { get; set; } = string.Empty;
}