using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class CreateProductRequestDTO
{
    // Product phải thuộc một Category
    [Range(1, int.MaxValue, ErrorMessage = "Danh mục không hợp lệ.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Tên sản phẩm không được để trống.")]
    [StringLength(150, ErrorMessage = "Tên sản phẩm không được vượt quá 150 ký tự.")]
    public string Name { get; set; } = string.Empty;

    // Nhận URL hoặc đường dẫn ảnh; hiện chưa triển khai upload file
    [StringLength(1000, ErrorMessage = "Đường dẫn ảnh không được vượt quá 1000 ký tự.")]
    public string? ImageUrl { get; set; }

    // Một sản phẩm phải có ít nhất hai lựa chọn size và giá
    [MinLength(2, ErrorMessage = "Sản phẩm phải có ít nhất hai size.")]
    public List<CreateProductSizeRequestDTO> ProductSizes { get; set; } = new();
}