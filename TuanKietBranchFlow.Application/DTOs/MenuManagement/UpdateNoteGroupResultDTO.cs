namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class UpdateNoteGroupResultDTO
{
    // Cho biết nhóm cần sửa không tồn tại hoặc đã xóa mềm
    public bool IsNoteGroupNotFound { get; set; }

    // Cho biết tên hoặc trạng thái gửi lên không hợp lệ
    public bool IsRequestInvalid { get; set; }

    // Cho biết một nhóm khác chưa xóa đã sử dụng tên mới
    public bool IsNameDuplicated { get; set; }

    // Chứa thông tin nhóm sau khi cập nhật thành công
    public MenuNoteGroupDTO? NoteGroup { get; set; }
}
