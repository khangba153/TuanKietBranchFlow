namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class CreateNoteOptionResultDTO
{
    // Tên, giá hoặc ID nhóm gửi lên không hợp lệ
    public bool IsRequestInvalid { get; set; }

    // Nhóm được chọn không tồn tại hoặc đã xóa mềm
    public bool IsNoteGroupNotFound { get; set; }

    // Tên đã thuộc một lựa chọn ghi chú khác chưa xóa
    public bool IsNameDuplicated { get; set; }

    // Chứa lựa chọn ghi chú vừa tạo khi lưu thành công
    public MenuNoteOptionDTO? NoteOption { get; set; }
}
