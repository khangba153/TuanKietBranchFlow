using TuanKietBranchFlow.Application.DTOs.MenuManagement.Toppings;

namespace TuanKietBranchFlow.Web.Models.MenuManagement.Toppings;

public class ToppingGroupUpdateApiResult
{
    // Cho biết request cập nhật có thành công hay không
    public bool IsSuccess { get; set; }
    // Giữ HTTP status code để giao diện xử lý khi cần
    public int StatusCode { get; set; }
    // Chứa nhóm và topping con sau khi cập nhật thành công
    public MenuToppingGroupDTO? ToppingGroup { get; set; }
    // Chứa thông báo lỗi để hiển thị trên giao diện
    public string ErrorMessage { get; set; } = string.Empty;
}
