namespace TuanKietBranchFlow.Application.DTOs.OrderMenus;

public class OrderMenuToppingGroupDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Chỉ chứa topping đang hoạt động và đang bán tại chi nhánh
    public List<OrderMenuToppingDTO> Toppings { get; set; }
        = new List<OrderMenuToppingDTO>();

}