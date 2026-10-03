using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class CreateSizeRequestDTO
{
    // ADMIN chỉ gửi tên; trạng thái và thông tin hệ thống do backend quản lý
    [Required(ErrorMessage = "Tên size không được để trống.")]
    [StringLength(50, ErrorMessage = "Tên size không được vượt quá 50 ký tự.")]
    public string Name { get; set; } = string.Empty;
}