namespace TuanKietBranchFlow.Web.Models;

public class NoteGroupDeleteApiResult
{
    // Cho biết request xóa nhóm ghi chú có thành công hay không
    public bool IsSuccess { get; set; }

    // Giữ HTTP status code để giao diện xử lý khi cần
    public int StatusCode { get; set; }

    // Chứa thông báo lỗi để hiển thị trên giao diện
    public string ErrorMessage { get; set; } = string.Empty;
}
