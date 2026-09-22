using TuanKietBranchFlow.Application.DTOs.Orders;

namespace TuanKietBranchFlow.Web.Models;

public class OrderCreateApiResult
{
    // Cho biết request tạo đơn có thành công không
    public bool IsSuccess { get; set; }

    // Giữ HTTP status code để giao diện xử lý khi cần
    public int StatusCode { get; set; }

    // Chứa đơn vừa tạo khi API trả thành công
    public CreateOrderResponseDTO? Order { get; set; }

    // Chứa thông báo đọc từ ProblemDetails khi thất bại
    public string ErrorMessage { get; set; } = string.Empty;
}