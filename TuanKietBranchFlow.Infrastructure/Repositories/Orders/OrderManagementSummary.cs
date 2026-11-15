namespace TuanKietBranchFlow.Infrastructure.Repositories.Orders;

// Tổng hợp toàn bộ phạm vi trước phân trang và trước bộ lọc trạng thái.
public class OrderManagementSummary
{
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int AdjustedOrders { get; set; }
    public int NeedsReviewOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal CompletedOrderValue { get; set; }
}
