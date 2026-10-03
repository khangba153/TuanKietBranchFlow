using TuanKietBranchFlow.Application.DTOs.MenuManagement;

namespace TuanKietBranchFlow.Web.Models;

public class ToppingUpdateApiResult
{
    // Cho biết request cập nhật topping có thành công hay không
    public bool IsSuccess { get; set; }

    // Giữ HTTP status code để giao diện xử lý khi cần
    public int StatusCode { get; set; }

    // Chứa topping sau khi cập nhật thành công
    public MenuToppingDTO? Topping { get; set; }

    // Chứa thông báo lỗi để hiển thị trên giao diện
    public string ErrorMessage { get; set; } = string.Empty;
}