using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Infrastructure.Data;
using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public class CategoryRepository : RepositoryBase<Category>, ICategoryRepository
{
    public CategoryRepository(BranchFlowDbContext context) : base(context)
    {
    }

    // Đọc cả danh mục hoạt động và tạm ngừng, trừ bản ghi đã xóa
    public async Task<List<Category>> GetAllNotDeletedAsync()
    {
        return await Context.Categories
            .AsNoTracking()
            .Where(category => !category.Deleted)
            .OrderBy(category => category.Id)
            .ToListAsync();

    }

    // Danh mục tạm ngưng vẫn giữ tên chỉ bỏ qua danh mục đã xóa
    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await Context.Categories
            .AnyAsync(category =>
                !category.Deleted
                && category.Name == name);
    }

    // Lấy Category đang được theo dõi để Service có thể cập nhật
    public async Task<Category?> GetNotDeletedByIdAsync(int categoryId)
    {
        return await Context.Categories
            .FirstOrDefaultAsync(category =>
                category.Id == categoryId
                && !category.Deleted);
    }

    // Kiểm tra tên đã được danh mục khác sử dụng chưa
    public async Task<bool> ExistsByNameExceptIdAsync(
        string name,
        int categoryId)
    {
        return await Context.Categories
            .AnyAsync(category =>
                !category.Deleted
                && category.Id != categoryId
                && category.Name == name);
    }
}
