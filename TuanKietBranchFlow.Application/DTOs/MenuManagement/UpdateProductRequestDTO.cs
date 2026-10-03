using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class UpdateProductRequestDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "Danh mục không hợp lệ.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Tên sản phẩm không được để trống.")]
    [StringLength(150, ErrorMessage = "Tên sản phẩm không được vượt quá 150 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Đường dẫn ảnh không được vượt quá 1000 ký tự.")]
    public string? ImageUrl { get; set; }

    [Required(ErrorMessage = "Trạng thái sản phẩm không được để trống.")]
    public bool? IsActive { get; set; }

    // Danh sách size–giá sau khi ADMIN chỉnh sửa.
    [Required(ErrorMessage = "Danh sách size không được để trống.")]
    [MinLength(2, ErrorMessage = "Sản phẩm phải có ít nhất hai size.")]
    public List<UpdateProductSizeRequestDTO> ProductSizes { get; set; } = new();
}