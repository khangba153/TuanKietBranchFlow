namespace TuanKietBranchFlow.Application.DTOs.Orders.Management;

public class OrderManagementPageDTO
{
    public List<OrderManagementListItemDTO> Items { get; set; } = new List<OrderManagementListItemDTO>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public OrderManagementSummaryDTO Summary { get; set; } = new OrderManagementSummaryDTO();
}
