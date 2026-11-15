namespace TuanKietBranchFlow.Application.DTOs.Orders.Management;

public class OrderManagementItemDTO
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SizeName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubtotalAmount { get; set; }
    public List<OrderManagementToppingDTO> Toppings { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}
