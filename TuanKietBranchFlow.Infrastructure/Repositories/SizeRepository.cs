using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Infrastructure.Data;
using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public class SizeRepository : RepositoryBase<Size>, ISizeRepository
{
    public SizeRepository(BranchFlowDbContext context) : base(context)
    {
    }

    // Lấy cả size đang hoạt động và tạm ngừng, trừ size đã xóa mềm
    public async Task<List<Size>> GetAllNotDeletedAsync()
    {
        return await Context.Sizes
            .AsNoTracking()
            .Where(size => !size.Deleted)
            .OrderBy(size => size.Id)
            .ToListAsync();
    }

    // Bỏ qua Size đã xóa mềm
    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await Context.Sizes
            .AnyAsync(size =>
                !size.Deleted
                && size.Name == name);
    }

    // Lấy Size chưa xóa theo Id
    public async Task<Size?> GetNotDeletedByIdAsync(int sizeId)
    {
        return await Context.Sizes
            .FirstOrDefaultAsync(size =>
                size.Id == sizeId
                && !size.Deleted);
    }

    // Kiểm tra tên trùng nhưng loại trừ chính Size đang được cập nhật.
    public async Task<bool> ExistsByNameExceptIdAsync(string name, int sizeId)
    {
        return await Context.Sizes
            .AnyAsync(size =>
                !size.Deleted
                && size.Id != sizeId
                && size.Name == name);
    }

    // Chỉ lấy các Size hợp lệ để gắn vào Product mới
    public async Task<List<Size>> GetActiveNotDeletedByIdsAsync(
        List<int> sizeIds)
    {
        return await Context.Sizes
            .AsNoTracking()
            .Where(size =>
                sizeIds.Contains(size.Id)
                && size.IsActive
                && !size.Deleted)
            .ToListAsync();
    }
}
