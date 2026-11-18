namespace TuanKietBranchFlow.Application.DTOs.Orders.Management;

public class OrderAdjustmentOptionsDTO
{
    public List<OrderAdjustmentSizeDTO> Sizes { get; set; } = new List<OrderAdjustmentSizeDTO>();
    public List<OrderAdjustmentToppingDTO> Toppings { get; set; } = new List<OrderAdjustmentToppingDTO>();
    public List<OrderAdjustmentNoteDTO> Notes { get; set; } = new List<OrderAdjustmentNoteDTO>();
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
