namespace TuanKietBranchFlow.Application.DTOs.Orders;

public class MyOrderDetailItemDTO
{
    public string ProductName { get; set; } = string.Empty;
    public string SizeName { get; set; } = string.Empty;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubtotalAmount { get; set; }

    public List<MyOrderDetailToppingDTO> Toppings { get; set; } =
        new List<MyOrderDetailToppingDTO>();

    public List<MyOrderDetailNoteDTO> Notes { get; set; } =
        new List<MyOrderDetailNoteDTO>();
}