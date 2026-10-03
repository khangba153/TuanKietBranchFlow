using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class UpdateNoteOptionRequestDTO
{
    // Tên lựa chọn ghi chú được phép cập nhật
    [Required(ErrorMessage = "Tên lựa chọn ghi chú không được để trống.")]
    [StringLength(100,
        ErrorMessage = "Tên lựa chọn ghi chú không được vượt quá 100 ký tự.")]
    public string Name { get; set; } = string.Empty;


    // Nullable để phân biệt trạng thái false với trường bị bỏ thiếu
    [Required(ErrorMessage = "Trạng thái hoạt động không được để trống.")]
    public bool? IsActive { get; set; }
}
