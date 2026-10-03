using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class UpdateToppingRequestDTO
{
    // Tên topping được phép cập nhật
    [Required(ErrorMessage = "Tên topping không được để trống.")]
    [StringLength(150,
        ErrorMessage = "Tên topping không được vượt quá 150 ký tự.")]
    public string Name { get; set; } = string.Empty;

    // Giá dùng chung của topping
    [Range(typeof(decimal), "0.01", "9999999999999999.99",
        ErrorMessage = "Giá topping phải lớn hơn 0 và trong giới hạn cho phép.")]
    public decimal Price { get; set; }

    // Nullable để phân biệt trạng thái false với trường bị bỏ thiếu
    [Required(ErrorMessage = "Trạng thái hoạt động không được để trống.")]
    public bool? IsActive { get; set; }
}