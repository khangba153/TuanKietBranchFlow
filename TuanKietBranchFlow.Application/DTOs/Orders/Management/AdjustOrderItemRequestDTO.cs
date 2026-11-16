using System.ComponentModel.DataAnnotations;
using TuanKietBranchFlow.Application.DTOs.Orders;

namespace TuanKietBranchFlow.Application.DTOs.Orders.Management;

public class AdjustOrderItemRequestDTO
{
    [Range(1, int.MaxValue)]
    public int OrderItemId { get; set; }

    [Range(1, int.MaxValue)]
    public int ProductSizeId { get; set; }

    [Range(1, 999)]
    public int Quantity { get; set; }

    [Required, MaxLength(100)]
    public List<CreateOrderItemToppingRequestDTO> Toppings { get; set; } = new();

    [Required, MaxLength(100)]
    public List<int> NoteOptionIds { get; set; } = new();
}
