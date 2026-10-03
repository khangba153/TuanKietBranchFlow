using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public interface INoteGroupRepository : IRepositoryBase<NoteGroup>
{
    // Lấy các nhóm chưa xóa cùng lựa chọn ghi chú chưa xóa cho màn quản lý
    Task<List<NoteGroup>> GetAllNotDeletedWithNoteOptionsAsync();

    // Kiểm tra tên đã thuộc một nhóm ghi chú chưa xóa hay chưa
    Task<bool> ExistsByNameAsync(string name);

    // Lấy nhóm chưa xóa cùng lựa chọn ghi chú để cập nhật và trả dữ liệu đầy đủ
    Task<NoteGroup?> GetNotDeletedByIdAsync(int noteGroupId);

    // Kiểm tra tên trùng, loại trừ chính nhóm đang sửa
    Task<bool> ExistsByNameExceptIdAsync(string name, int noteGroupId);
}
