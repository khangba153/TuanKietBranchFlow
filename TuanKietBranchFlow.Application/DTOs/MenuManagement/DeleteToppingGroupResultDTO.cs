namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class DeleteToppingGroupResultDTO
{
    // Nhóm không tồn tại hoặc đã bị xóa mềm
    public bool IsToppingGroupNotFound { get; set; }

    // Xác nhận nhóm đã được xóa mềm và lưu thành công
    public bool IsDeleted { get; set; }
}