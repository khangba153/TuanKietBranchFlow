using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Infrastructure.Data;
using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public class NoteGroupRepository
    : RepositoryBase<NoteGroup>, INoteGroupRepository
{
    public NoteGroupRepository(BranchFlowDbContext context) : base(context)
    {
    }

    // Đọc nhóm và lựa chọn ghi chú, giữ cả bản ghi tạm ngừng nhưng loại bản ghi đã xóa
    public async Task<List<NoteGroup>> GetAllNotDeletedWithNoteOptionsAsync()
    {
        return await Context.NoteGroups
            .AsNoTracking()
            .Where(group => !group.Deleted)
            .Include(group => group.NoteOptions
                .Where(noteOption => !noteOption.Deleted))
            .OrderBy(group => group.Id)
            .ToListAsync();
    }

    // Nhóm tạm ngừng vẫn giữ tên; chỉ bỏ qua nhóm đã xóa mềm
    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await Context.NoteGroups
            .AnyAsync(group =>
                !group.Deleted
                && group.Name == name);
    }

    // Lấy nhóm có tracking để Service cập nhật tên và trạng thái
    public async Task<NoteGroup?> GetNotDeletedByIdAsync(int noteGroupId)
    {
        return await Context.NoteGroups
            .Include(group => group.NoteOptions
                .Where(noteOption => !noteOption.Deleted))
            .FirstOrDefaultAsync(group =>
                group.Id == noteGroupId
                && !group.Deleted);
    }

    // Nhóm tạm ngừng vẫn giữ tên; không tính chính nhóm đang sửa
    public async Task<bool> ExistsByNameExceptIdAsync(
        string name,
        int noteGroupId)
    {
        return await Context.NoteGroups
            .AnyAsync(group =>
                !group.Deleted
                && group.Id != noteGroupId
                && group.Name == name);
    }
}
