namespace TuanKietBranchFlow.Application.DTOs.Orders;

public class MyOrderDetailToppingDTO
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}