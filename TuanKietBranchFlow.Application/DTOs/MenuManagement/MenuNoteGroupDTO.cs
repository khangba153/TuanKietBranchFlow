namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class MenuNoteGroupDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    // Lựa chọn chưa xóa thuộc nhóm, gồm cả lựa chọn đang tạm ngừng
    public List<MenuNoteOptionDTO> NoteOptions { get; set; } =
        new List<MenuNoteOptionDTO>();
}
