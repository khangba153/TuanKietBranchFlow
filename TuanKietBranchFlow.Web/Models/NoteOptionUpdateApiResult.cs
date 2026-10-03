using TuanKietBranchFlow.Application.DTOs.MenuManagement;

namespace TuanKietBranchFlow.Web.Models;

public class NoteOptionUpdateApiResult
{
    // Cho biết request cập nhật lựa chọn ghi chú có thành công hay không
    public bool IsSuccess { get; set; }

    // Giữ HTTP status code để giao diện xử lý khi cần
    public int StatusCode { get; set; }

    // Chứa lựa chọn ghi chú sau khi cập nhật thành công
    public MenuNoteOptionDTO? NoteOption { get; set; }

    // Chứa thông báo lỗi để hiển thị trên giao diện
    public string ErrorMessage { get; set; } = string.Empty;
}
