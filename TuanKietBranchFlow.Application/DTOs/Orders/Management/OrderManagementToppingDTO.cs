namespace TuanKietBranchFlow.Application.DTOs.Orders.Management;

public class OrderManagementToppingDTO
{
    public int ToppingId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
