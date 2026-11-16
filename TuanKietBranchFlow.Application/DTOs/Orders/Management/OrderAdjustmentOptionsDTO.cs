namespace TuanKietBranchFlow.Application.DTOs.Orders.Management;

public class OrderAdjustmentOptionsDTO
{
    public List<OrderAdjustmentSizeDTO> Sizes { get; set; } = new();
    public List<OrderAdjustmentToppingDTO> Toppings { get; set; } = new();
    public List<OrderAdjustmentNoteDTO> Notes { get; set; } = new();
}

public class OrderAdjustmentSizeDTO
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

public class OrderAdjustmentToppingDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int MaxQuantity { get; set; }
}

public class OrderAdjustmentNoteDTO
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string GroupName { get; set; } = "";
    public string Name { get; set; } = "";
}
