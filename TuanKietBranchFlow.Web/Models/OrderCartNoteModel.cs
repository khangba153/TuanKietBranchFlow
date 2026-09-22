namespace TuanKietBranchFlow.Web.Models;

public class OrderCartNoteModel
{
    // Id thật sẽ được gửi cho API khi tạo đơn
    public int NoteOptionId { get; set; }

    // Tên dùng để hiển thị trong giỏ hàng
    public string Name { get; set; } = string.Empty;
}