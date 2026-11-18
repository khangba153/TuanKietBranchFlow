namespace TuanKietBranchFlow.Application.DTOs.Orders;

public class MyOrderResolutionDTO
{
    public int Id { get; set; }
    public string ProcessedByFullName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? AfterStatus { get; set; }
    public decimal? BeforeTotalAmount { get; set; }
    public decimal? AfterTotalAmount { get; set; }
    public List<MyOrderDetailItemDTO> BeforeItems { get; set; } = new List<MyOrderDetailItemDTO>();
    public List<MyOrderDetailItemDTO> AfterItems { get; set; } = new List<MyOrderDetailItemDTO>();
}
