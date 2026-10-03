using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class CreateCategoryRequestDTO
{
    // Người quản trị chỉ nhập tên; các trường hệ thống do backen quản lý
    [Required(ErrorMessage = "Tên danh mục không được để trống.")]
    [StringLength(150, ErrorMessage = "Tên danh mục không được vượt quá 150 ký tự.")]
    public string Name { get ; set; } = string.Empty;
}