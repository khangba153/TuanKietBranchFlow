namespace TuanKietBranchFlow.Application.DTOs.Orders.Management;

public class OrderAdjustmentResultDTO
{
    public bool IsForbidden { get; set; }
    public bool IsNotFound { get; set; }
    public bool IsConflict { get; set; }
    public string? Error { get; set; }
    public OrderManagementDetailDTO? Before { get; set; }
    public OrderManagementDetailDTO? After { get; set; }
    public string? PreviewHash { get; set; }
    public OrderAdjustmentOptionsDTO? Options { get; set; }
}
