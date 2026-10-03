using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class CreateNoteGroupRequestDTO
{
    // ADMIN chỉ gửi tên nhóm; thông tin hệ thống do backend quản lý
    [Required(ErrorMessage = "Tên nhóm ghi chú không được để trống.")]
    [StringLength(100,
        ErrorMessage = "Tên nhóm ghi chú không được vượt quá 100 ký tự.")]
    public string Name { get; set; } = string.Empty;
}
