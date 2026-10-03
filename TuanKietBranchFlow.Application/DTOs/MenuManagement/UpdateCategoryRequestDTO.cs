using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class UpdateCategoryRequestDTO
{
    // Chỉ nhận 2 thông tin ADMIN được phép chỉnh sửa
    [Required(ErrorMessage = "Tên danh mục không được để trống.")]
    [StringLength(150, ErrorMessage = "Tên danh mục không được vượt quá 150 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Trạng thái hoạt động không được để trống.")]
    public bool? IsActive { get; set; }
}