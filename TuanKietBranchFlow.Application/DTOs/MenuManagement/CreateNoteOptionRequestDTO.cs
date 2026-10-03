using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class CreateNoteOptionRequestDTO
{
    // Nhóm được chọn trên Web để thêm lựa chọn ghi chú vào
    [Range(1, int.MaxValue, ErrorMessage = "Nhóm ghi chú không hợp lệ.")]
    public int NoteGroupId { get; set; }

    // Tên lựa chọn ghi chú theo giới hạn của database
    [Required(ErrorMessage = "Tên lựa chọn ghi chú không được để trống.")]
    [StringLength(100,
        ErrorMessage = "Tên lựa chọn ghi chú không được vượt quá 100 ký tự.")]
    public string Name { get; set; } = string.Empty;

}
