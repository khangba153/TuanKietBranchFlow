namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class UpdateNoteOptionResultDTO
{
    // Lựa chọn ghi chú không tồn tại hoặc đã xóa mềm
    public bool IsNoteOptionNotFound { get; set; }

    // Tên, giá hoặc trạng thái gửi lên không hợp lệ
    public bool IsRequestInvalid { get; set; }

    // Một lựa chọn ghi chú khác chưa xóa đã sử dụng tên mới
    public bool IsNameDuplicated { get; set; }

    // Chứa lựa chọn ghi chú sau khi cập nhật thành công
    public MenuNoteOptionDTO? NoteOption { get; set; }
}
