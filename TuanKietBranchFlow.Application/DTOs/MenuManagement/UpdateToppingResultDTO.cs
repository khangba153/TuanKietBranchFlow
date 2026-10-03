namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class UpdateToppingResultDTO
{
    // Topping không tồn tại hoặc đã xóa mềm
    public bool IsToppingNotFound { get; set; }

    // Tên, giá hoặc trạng thái gửi lên không hợp lệ
    public bool IsRequestInvalid { get; set; }

    // Một topping khác chưa xóa đã sử dụng tên mới
    public bool IsNameDuplicated { get; set; }

    // Chứa topping sau khi cập nhật thành công
    public MenuToppingDTO? Topping { get; set; }
}