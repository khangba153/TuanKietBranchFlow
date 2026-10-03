using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public interface INoteOptionRepository : IRepositoryBase<NoteOption>
{
    // Kiểm tra tên lựa chọn ghi chú chưa xóa trên trong cùng nhóm
    Task<bool> ExistsByNameAsync(string name, int noteGroupId);

    // Lấy lựa chọn ghi chú còn thuộc nhóm chưa xóa để cập nhật
    Task<NoteOption?> GetNotDeletedByIdAsync(int noteOptionId);

    // Kiểm tra tên trùng, loại chính lựa chọn ghi chú đang sửa
    Task<bool> ExistsByNameExceptIdAsync(string name, int noteOptionId, int noteGroupId);
}
