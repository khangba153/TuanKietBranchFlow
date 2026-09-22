namespace TuanKietBranchFlow.Application.DTOs.Orders;

public class MyOrderDetailDTO
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;

    public int BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;

    public DateOnly BusinessDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;

    public string? ReportReason { get; set; }
    public DateTime? ReportedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<MyOrderDetailItemDTO> Items { get; set; } =
        new List<MyOrderDetailItemDTO>();
}