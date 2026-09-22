using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.Orders;

public class CreateOrderRequestDTO
{
    // Xác nhận chi nhánh nhận đơn
    [Range(1, int.MaxValue, ErrorMessage = "Chi nhánh không hợp lệ.")]
    public int BranchId { get; set; }

    // Một đơn phải có ít nhất một dòng món
    [Required(ErrorMessage = "Danh sách món không được để trống.")]
    [MinLength(1, ErrorMessage = "Đơn hàng phải có ít nhất một món.")]
    public List<CreateOrderItemRequestDTO> Items { get; set; } =
        new List<CreateOrderItemRequestDTO>();
}