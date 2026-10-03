using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class UpdateNoteGroupRequestDTO
{
    // Chỉ nhận tên và trạng thái hoạt động được phép cập nhật
    [Required(ErrorMessage = "Tên nhóm ghi chú không được để trống.")]
    [StringLength(100,
        ErrorMessage = "Tên nhóm ghi chú không được vượt quá 100 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Trạng thái hoạt động không được để trống.")]
    public bool? IsActive { get; set; }
}
