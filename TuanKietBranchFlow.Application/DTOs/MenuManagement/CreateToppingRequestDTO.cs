using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class CreateToppingRequestDTO
{
    // Nhóm được chọn trên Web để thêm topping vào
    [Range(1, int.MaxValue, ErrorMessage = "Nhóm topping không hợp lệ.")]
    public int ToppingGroupId { get; set; }

    // Tên topping theo giới hạn của database
    [Required(ErrorMessage = "Tên topping không được để trống.")]
    [StringLength(150,
        ErrorMessage = "Tên topping không được vượt quá 150 ký tự.")]
    public string Name { get; set; } = string.Empty;

    // Giá dùng chung, cùng quy tắc giá dương như ProductSize
    [Range(typeof(decimal), "0.01", "9999999999999999.99",
        ErrorMessage = "Giá topping phải lớn hơn 0.")]
    public decimal Price { get; set; }
}