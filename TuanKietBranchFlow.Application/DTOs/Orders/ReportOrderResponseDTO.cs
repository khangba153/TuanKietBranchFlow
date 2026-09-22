namespace TuanKietBranchFlow.Application.DTOs.Orders;

public class ReportOrderResponseDTO
{
    public int Id { get; set; }
    public string Code { get ; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ReportReason { set; get; } = string.Empty;
    public DateTime ReportedAt { get; set; }
}