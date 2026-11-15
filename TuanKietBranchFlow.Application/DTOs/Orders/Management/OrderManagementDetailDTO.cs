namespace TuanKietBranchFlow.Application.DTOs.Orders.Management;

public class OrderManagementDetailDTO
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public string CreatedByFullName { get; set; } = string.Empty;
    public DateOnly BusinessDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public int TotalQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ReportReason { get; set; }
    public DateTime? ReportedAt { get; set; }
    public string? ReportedByFullName { get; set; }
    public List<OrderManagementItemDTO> Items { get; set; } = new();
}
