namespace TuanKietBranchFlow.Application.DTOs.OrderMenus;

public class OrderMenuToppingDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }

    // Topping thường tối đa 3, up-size tối đa 1
    public int MaxQuantity { get; set; }
}