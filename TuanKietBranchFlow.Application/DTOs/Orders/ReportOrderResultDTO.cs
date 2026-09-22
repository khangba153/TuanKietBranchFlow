namespace TuanKietBranchFlow.Application.DTOs.Orders;

public class ReportOrderResult
{
    // Không tìm thấy đơn thuộc employee hiện tại
    public bool IsOrderFound { get; set; }

    // Chỉ đơn COMPLETED mới được báo sai
    public bool CanReport { get; set; }

    // Giải thích lỗi nghiệp vụ để Controller tạo ProblemDetails
    public string? ErrorMessage { get; set; }

    // Lý do báo sai phải có nội dung hợp lệ
    public bool IsReportValid { get; set; }

    // Chỉ có dữ liệu khi cập nhật thành công
    public ReportOrderResponseDTO? Order { get; set; }
}