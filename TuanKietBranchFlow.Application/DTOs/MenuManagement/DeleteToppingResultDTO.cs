namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class DeleteToppingResultDTO
{
    // Topping không tồn tại, đã xóa hoặc thuộc nhóm đã xóa
    public bool IsToppingNotFound { get; set; }

    // Xác nhận topping đã được xóa mềm và lưu thành công
    public bool IsDeleted { get; set; }
}