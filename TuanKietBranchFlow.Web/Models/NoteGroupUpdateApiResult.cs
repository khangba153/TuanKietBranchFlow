using TuanKietBranchFlow.Application.DTOs.MenuManagement;

namespace TuanKietBranchFlow.Web.Models;

public class NoteGroupUpdateApiResult
{
    // Cho biết request cập nhật có thành công hay không
    public bool IsSuccess { get; set; }

    // Giữ HTTP status code để giao diện xử lý khi cần
    public int StatusCode { get; set; }

    // Chứa nhóm và lựa chọn ghi chú con sau khi cập nhật thành công
    public MenuNoteGroupDTO? NoteGroup { get; set; }

    // Chứa thông báo lỗi để hiển thị trên giao diện
    public string ErrorMessage { get; set; } = string.Empty;
}
