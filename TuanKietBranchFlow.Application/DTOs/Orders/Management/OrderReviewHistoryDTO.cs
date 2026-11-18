namespace TuanKietBranchFlow.Application.DTOs.Orders.Management;

public class OrderReviewHistoryDTO
{
    public int Id { get; set; }
    public string ProcessedByFullName { get; set; } = "";
    public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string? BeforeStatus { get; set; }
    public string? AfterStatus { get; set; }
    public decimal? BeforeTotalAmount { get; set; }
    public decimal? AfterTotalAmount { get; set; }
    public List<OrderManagementItemDTO> BeforeItems { get; set; } = new List<OrderManagementItemDTO>();
    public List<OrderManagementItemDTO> AfterItems { get; set; } = new List<OrderManagementItemDTO>();
}
