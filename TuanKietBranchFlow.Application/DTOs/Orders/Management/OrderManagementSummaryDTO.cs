namespace TuanKietBranchFlow.Application.DTOs.Orders.Management;

public class OrderManagementSummaryDTO
{
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int AdjustedOrders { get; set; }
    public int NeedsReviewOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal CompletedOrderValue { get; set; }
}
