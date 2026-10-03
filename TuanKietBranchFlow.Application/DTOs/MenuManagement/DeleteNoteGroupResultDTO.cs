namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class DeleteNoteGroupResultDTO
{
    // Nhóm không tồn tại hoặc đã bị xóa mềm
    public bool IsNoteGroupNotFound { get; set; }

    // Xác nhận nhóm đã được xóa mềm và lưu thành công
    public bool IsDeleted { get; set; }
}
