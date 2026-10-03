using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Infrastructure.Data;
using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public class ToppingRepository : RepositoryBase<Topping>, IToppingRepository
{
    public ToppingRepository(BranchFlowDbContext context) : base(context)
    {
    }

    // Topping tạm ngừng vẫn giữ tên; chỉ bỏ qua topping đã xóa
    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await Context.Toppings
            .AnyAsync(topping =>
                !topping.Deleted
                && topping.Name == name);
    }

    // Truy vấn có tracking; topping thuộc nhóm đã xóa không được sửa
    public async Task<Topping?> GetNotDeletedByIdAsync(int toppingId)
    {
        return await Context.Toppings
            .FirstOrDefaultAsync(topping =>
                topping.Id == toppingId
                && !topping.Deleted
                && !topping.ToppingGroup.Deleted);
    }

    // Tên duy nhất toàn hệ thống, không tính chính topping đang sửa
    public async Task<bool> ExistsByNameExceptIdAsync(
        string name,
        int toppingId)
    {
        return await Context.Toppings
            .AnyAsync(topping =>
                !topping.Deleted
                && topping.Id != toppingId
                && topping.Name == name);
    }
}