namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class CreateToppingResultDTO
{
    // Tên, giá hoặc ID nhóm gửi lên không hợp lệ
    public bool IsRequestInvalid { get; set; }

    // Nhóm được chọn không tồn tại hoặc đã xóa mềm
    public bool IsToppingGroupNotFound { get; set; }

    // Tên đã thuộc một topping khác chưa xóa
    public bool IsNameDuplicated { get; set; }

    // Chứa topping vừa tạo khi lưu thành công
    public MenuToppingDTO? Topping { get; set; }
}