using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Infrastructure.Data;
using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public class OrderMenuRepository : IOrderMenuRepository
{
    private readonly BranchFlowDbContext Context;

    public OrderMenuRepository(BranchFlowDbContext context)
    {
        Context = context;
    }

    // Lấy các danh mục có ít nhất 1 món và 1 size đang bán
    public async Task<List<Category>> GetAvailableCategoriesAsync(int branchId)
    {
        return await Context.Categories
            .AsNoTracking()
            .Where(category =>
                category.IsActive
                && !category.Deleted
                && category.Products.Any(product =>
                    product.IsActive
                    && !product.Deleted
                    && product.BranchProducts.Any(branchProduct =>
                        branchProduct.BranchId == branchId
                        && branchProduct.IsAvailable)
                    && product.ProductSizes.Any(productSize =>
                        productSize.IsActive
                        && !productSize.Deleted
                        && productSize.Size.IsActive
                        && !productSize.Size.Deleted)))
            .Include(category => category.Products
                .Where(product =>
                    product.IsActive
                    && !product.Deleted
                    && product.BranchProducts.Any(branchProduct =>
                        branchProduct.BranchId == branchId
                        && branchProduct.IsAvailable)
                    && product.ProductSizes.Any(productSize =>
                        productSize.IsActive
                        && !productSize.Deleted
                        && productSize.Size.IsActive
                        && !productSize.Size.Deleted))
                .OrderBy(product => product.Id))
            .ThenInclude(product => product.ProductSizes
                .Where(productSize =>
                    productSize.IsActive
                    && !productSize.Deleted
                    && productSize.Size.IsActive
                    && !productSize.Size.Deleted)
                .OrderBy(productSize => productSize.Price))
            .ThenInclude(productSize => productSize.Size)
            .OrderBy(category => category.Id)
            .ToListAsync();
    }

    // Lấy các nhóm topping có ít nhất một topping đang bán
    public async Task<List<ToppingGroup>> GetAvailableToppingGroupsAsync(
        int branchId)
    {
        return await Context.ToppingGroups
            .AsNoTracking()
            .Where(group =>
                group.IsActive
                && !group.Deleted
                && group.Toppings.Any(topping =>
                    topping.IsActive
                    && !topping.Deleted
                    && topping.BranchToppings.Any(branchTopping =>
                        branchTopping.BranchId == branchId
                        && branchTopping.IsAvailable)))
            .Include(group => group.Toppings
                .Where(topping =>
                    topping.IsActive
                    && !topping.Deleted
                    && topping.BranchToppings.Any(branchTopping =>
                        branchTopping.BranchId == branchId
                        && branchTopping.IsAvailable))
                .OrderBy(toppping => toppping.Id))
            .OrderBy(group => group.Id)
            .ToListAsync();
    }

    // Lấy các nhóm ghi chú có ít nhất 1 lựa chọn đang hoạt động
    public async Task<List<NoteGroup>> GetActiveNoteGroupsAsync()
    {
        return await Context.NoteGroups
            .AsNoTracking()
            .Where(group =>
                group.IsActive
                && !group.Deleted
                && group.NoteOptions.Any(option =>
                    option.IsActive
                    && !option.Deleted))
            .Include(group => group.NoteOptions
                .Where(option =>
                    option.IsActive
                    && !option.Deleted)
                .OrderBy(option => option.Id))
            .OrderBy(group => group.Id)
            .ToListAsync();
    }
}