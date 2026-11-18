using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories.Orders;

// Dữ liệu đọc đã phân trang; không trả IQueryable ra khỏi repository.
public class OrderManagementPage
{
    public List<SalesOrder> Items { get; set; } = new List<SalesOrder>();
    public int TotalCount { get; set; }
    public OrderManagementSummary Summary { get; set; } = new OrderManagementSummary();
}
