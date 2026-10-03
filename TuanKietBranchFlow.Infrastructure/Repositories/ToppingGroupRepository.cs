using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Infrastructure.Data;
using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public class ToppingGroupRepository
    : RepositoryBase<ToppingGroup>, IToppingGroupRepository
{
    public ToppingGroupRepository(BranchFlowDbContext context) : base(context)
    {
    }

    // Đọc nhóm và topping, giữ cả bản ghi tạm ngừng nhưng loại bản ghi đã xóa
    public async Task<List<ToppingGroup>> GetAllNotDeletedWithToppingsAsync()
    {
        return await Context.ToppingGroups
            .AsNoTracking()
            .Where(group => !group.Deleted)
            .Include(group => group.Toppings
                .Where(topping => !topping.Deleted))
            .OrderBy(group => group.Id)
            .ToListAsync();
    }

    // Nhóm tạm ngừng vẫn giữ tên; chỉ bỏ qua nhóm đã xóa mềm
    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await Context.ToppingGroups
            .AnyAsync(group =>
                !group.Deleted
                && group.Name == name);
    }

    // Lấy nhóm có tracking để Service cập nhật tên và trạng thái
    public async Task<ToppingGroup?> GetNotDeletedByIdAsync(int toppingGroupId)
    {
        return await Context.ToppingGroups
            .Include(group => group.Toppings
                .Where(topping => !topping.Deleted))
            .FirstOrDefaultAsync(group =>
                group.Id == toppingGroupId
                && !group.Deleted);
    }

    // Nhóm tạm ngừng vẫn giữ tên; không tính chính nhóm đang sửa
    public async Task<bool> ExistsByNameExceptIdAsync(
        string name,
        int toppingGroupId)
    {
        return await Context.ToppingGroups
            .AnyAsync(group =>
                !group.Deleted
                && group.Id != toppingGroupId
                && group.Name == name);
    }
}