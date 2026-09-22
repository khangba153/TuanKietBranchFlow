using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.Orders;

public class CreateOrderItemToppingRequestDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "Topping không hợp lệ.")]
    public int ToppingId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Số lượng topping phải lớn hơn 0.")]
    public int Quantity { get; set; }
}