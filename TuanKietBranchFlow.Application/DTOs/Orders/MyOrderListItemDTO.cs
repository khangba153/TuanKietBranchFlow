namespace TuanKietBranchFlow.Application.DTOs.Orders;

public class MyOrderListItemDTO
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }

    // Tổng số món thực tế, không phải số dòng OrderItem
    public int TotalQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}