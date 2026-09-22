using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.Orders;

public class CreateOrderItemRequestDTO
{
    // Xác định chính xác món, size và bản ghi giá cần đọc lại
    [Range(1, int.MaxValue, ErrorMessage = "ProductSize không hợp lệ.")]
    public int ProductSizeId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Số lượng món phải lớn hơn 0.")]
    public int Quantity { get; set; }

    // Topping là tùy chọn của riêng từng dòng món
    public List<CreateOrderItemToppingRequestDTO> Toppings { get; set; } =
        new List<CreateOrderItemToppingRequestDTO>();

    // Ghi chú chỉ gửi ID; Backend đọc lại tên và nhóm từ database
    public List<int> NoteOptionIds { get; set; } =
        new List<int>();
}