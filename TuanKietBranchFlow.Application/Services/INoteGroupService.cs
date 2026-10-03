using TuanKietBranchFlow.Application.DTOs.MenuManagement;

namespace TuanKietBranchFlow.Application.Services;

public interface INoteGroupService
{
    // Lấy các nhóm ghi chú và lựa chọn ghi chú để ADMIN quản lý, OWNER xem
    Task<List<MenuNoteGroupDTO>> GetAllNoteGroupsAsync();

    // Tạo nhóm ghi chú mới
    Task<CreateNoteGroupResultDTO> CreateNoteGroupAsync(
        CreateNoteGroupRequestDTO request);

    // Cập nhật tên và trạng thái của nhóm ghi chú
    Task<UpdateNoteGroupResultDTO> UpdateNoteGroupAsync(
        int noteGroupId,
        UpdateNoteGroupRequestDTO request);

    // Xóa mềm nhóm ghi chú, giữ nguyên dữ liệu lựa chọn ghi chú con
    Task<DeleteNoteGroupResultDTO> DeleteNoteGroupAsync(
        int noteGroupId);
}
