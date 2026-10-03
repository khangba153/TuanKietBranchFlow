using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Infrastructure.Data;
using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public class ProductRepository : RepositoryBase<Product>, IProductRepository
{
    public ProductRepository(BranchFlowDbContext context) : base(context)
    {
    }

    // Lấy Product, Category và các size–giá chưa xóa cho màn quản lý
    public async Task<List<Product>> GetAllNotDeletedAsync()
    {
        return await Context.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.ProductSizes
                .Where(productSize => !productSize.Deleted))
            .ThenInclude(productSize => productSize.Size)
            .Where(product => !product.Deleted)
            .OrderBy(product => product.Id)
            .ToListAsync();
    }

    // Kiểm tra tên đã được Product chưa xóa sử dụng chưa
    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await Context.Products
            .AnyAsync(product =>
                !product.Deleted
                && product.Name == name);
    }

    // Lấy Product đang được theo dõi cùng Category và các size–giá chưa xóa
    public async Task<Product?> GetNotDeletedByIdAsync(int productId)
    {
        return await Context.Products
            .Include(product => product.Category)
            .Include(product => product.ProductSizes
                .Where(productSize => !productSize.Deleted))
            .ThenInclude(productSize => productSize.Size)
            .FirstOrDefaultAsync(product =>
                product.Id == productId
                && !product.Deleted);
    }

    // Kiểm tra tên đã được Product khác chưa xóa sử dụng chưa
    public async Task<bool> ExistsByNameExceptIdAsync(
        string name,
        int productId)
    {
        return await Context.Products
            .AnyAsync(product =>
                !product.Deleted
                && product.Id != productId
                && product.Name == name);
    }
}