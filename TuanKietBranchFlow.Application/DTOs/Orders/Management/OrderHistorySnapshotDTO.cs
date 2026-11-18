namespace TuanKietBranchFlow.Application.DTOs.Orders.Management;

// Chỉ giải mã các trường được phép hiển thị, không trả raw JSON audit ra client.
public class OrderHistorySnapshotDTO
{
    public string? Status { get; set; }
    public decimal? TotalAmount { get; set; }
    public List<OrderManagementItemDTO> Items { get; set; } = new List<OrderManagementItemDTO>();
}
