namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class DeleteNoteOptionResultDTO
{
    // Lựa chọn ghi chú không tồn tại, đã xóa hoặc thuộc nhóm đã xóa
    public bool IsNoteOptionNotFound { get; set; }

    // Xác nhận lựa chọn ghi chú đã được xóa mềm và lưu thành công
    public bool IsDeleted { get; set; }
}
