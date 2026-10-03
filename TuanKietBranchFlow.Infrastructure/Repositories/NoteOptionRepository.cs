using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Infrastructure.Data;
using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public class NoteOptionRepository : RepositoryBase<NoteOption>, INoteOptionRepository
{
    public NoteOptionRepository(BranchFlowDbContext context) : base(context)
    {
    }

    // Lựa chọn ghi chú tạm ngừng vẫn giữ tên; chỉ bỏ qua lựa chọn ghi chú đã xóa
    public async Task<bool> ExistsByNameAsync(string name, int noteGroupId)
    {
        return await Context.NoteOptions
            .AnyAsync(noteOption =>
                !noteOption.Deleted
                && noteOption.NoteGroupId == noteGroupId
                && noteOption.Name == name);
    }

    // Truy vấn có tracking; lựa chọn ghi chú thuộc nhóm đã xóa không được sửa
    public async Task<NoteOption?> GetNotDeletedByIdAsync(int noteOptionId)
    {
        return await Context.NoteOptions
            .FirstOrDefaultAsync(noteOption =>
                noteOption.Id == noteOptionId
                && !noteOption.Deleted
                && !noteOption.NoteGroup.Deleted);
    }

    // Tên duy nhất trong cùng nhóm, không tính chính lựa chọn ghi chú đang sửa
    public async Task<bool> ExistsByNameExceptIdAsync(
        string name,
        int noteOptionId,
        int noteGroupId)
    {
        return await Context.NoteOptions
            .AnyAsync(noteOption =>
                !noteOption.Deleted
                && noteOption.NoteGroupId == noteGroupId
                && noteOption.Id != noteOptionId
                && noteOption.Name == name);
    }
}
