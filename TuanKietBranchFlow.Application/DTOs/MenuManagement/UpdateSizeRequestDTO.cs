using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class UpdateSizeRequestDTO
{
    // Chỉ nhận tên và trạng thái hoạt động được phép cập nhật
    [Required(ErrorMessage = "Tên size không được để trống.")]
    [StringLength(50, ErrorMessage = "Tên size không được vượt quá 50 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Trạng thái hoạt động không được để trống.")]
    public bool? IsActive { get; set; }
}