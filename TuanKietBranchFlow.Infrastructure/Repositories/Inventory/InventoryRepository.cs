using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Infrastructure.Data;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories.Common;

namespace TuanKietBranchFlow.Infrastructure.Repositories.Inventory;

public class InventoryRepository : RepositoryBase<BranchIngredient>, IInventoryRepository
{
    public InventoryRepository(BranchFlowDbContext context) : base(context)
    {
    }

    // Khóa ngắn trong transaction để tạo nguyên liệu và tắt đơn vị không vượt nhau.
    // Khóa tự giải phóng khi UnitOfWork commit/rollback, không khóa các lượt xem tồn.
    public async Task LockConfigurationAsync()
    {
        await Context.Database.ExecuteSqlRawAsync("""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource=N'BranchFlow.InventoryConfiguration',
                @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=5000;
            IF @result < 0 THROW 51001, N'Inventory configuration is busy.', 1;
            """);
    }

    public async Task<List<IngredientUnit>> GetUnitsAsync(string? search)
    {
        return await Context.IngredientUnits.AsNoTracking().Where(unit => !unit.Deleted && (search == null || unit.Name.Contains(search))).OrderBy(unit => unit.Name).ThenBy(unit => unit.Id).ToListAsync();
    }

    public async Task<List<Ingredient>> GetIngredientsAsync(string? search)
    {
        return await Context.Ingredients.AsNoTracking().Include(ingredient => ingredient.Unit).Where(ingredient => !ingredient.Deleted && (search == null || ingredient.Name.Contains(search))).OrderBy(ingredient => ingredient.Name).ThenBy(ingredient => ingredient.Id).ToListAsync();
    }

    public Task<IngredientUnit?> GetUnitAsync(int id)
    {
        return Context.IngredientUnits.FirstOrDefaultAsync(unit => unit.Id == id && !unit.Deleted);
    }

    public Task<Ingredient?> GetIngredientAsync(int id)
    {
        return Context.Ingredients.FirstOrDefaultAsync(ingredient => ingredient.Id == id && !ingredient.Deleted);
    }

    public Task<bool> UnitNameExistsAsync(
        string name,
        int exceptId)
    {
        return Context.IngredientUnits.AnyAsync(unit => !unit.Deleted && unit.Id != exceptId && unit.Name == name);
    }

    public Task<bool> IngredientNameExistsAsync(
        string name,
        int exceptId)
    {
        return Context.Ingredients.AnyAsync(ingredient => !ingredient.Deleted && ingredient.Id != exceptId && ingredient.Name == name);
    }

    public Task<bool> UnitIsUsedAsync(int id)
    {
        return Context.Ingredients.AnyAsync(ingredient => ingredient.UnitId == id);
    }

    public Task<bool> UnitHasActiveIngredientsAsync(int id)
    {
        return Context.Ingredients.AnyAsync(ingredient => ingredient.UnitId == id && !ingredient.Deleted && ingredient.IsActive);
    }

    public Task<bool> IngredientIsUsedAsync(int id)
    {
        return Context.Ingredients.AnyAsync(ingredient => ingredient.Id == id && (ingredient.BranchIngredients.Any() || ingredient.StockTransactionDetails.Any() || ingredient.StocktakeItems.Any()));
    }

    public Task<List<int>> GetUsedUnitIdsAsync()
    {
        return Context.Ingredients.Select(ingredient => ingredient.UnitId).Distinct().ToListAsync();
    }

    public Task<List<int>> GetUsedIngredientIdsAsync()
    {
        return Context.Ingredients.Where(ingredient => ingredient.BranchIngredients.Any() || ingredient.StockTransactionDetails.Any() || ingredient.StocktakeItems.Any()).Select(ingredient => ingredient.Id).ToListAsync();
    }

    public async Task AddUnitAsync(IngredientUnit unit)
    {
        await Context.IngredientUnits.AddAsync(unit);
    }

    public async Task AddIngredientAsync(Ingredient ingredient)
    {
        await Context.Ingredients.AddAsync(ingredient);
    }

    public Task<BranchIngredient?> GetConfigurationAsync(int id)
    {
        return Context.BranchIngredients.FirstOrDefaultAsync(configuration => configuration.Id == id);
    }

    public Task<bool> ConfigurationExistsAsync(
        int branchId,
        int ingredientId)
    {
        return Context.BranchIngredients.AnyAsync(configuration => configuration.BranchId == branchId && configuration.IngredientId == ingredientId);
    }

    public void SetExpectedRowVersion(
        BranchIngredient configuration,
        byte[] rowVersion)
    {
        Context.Entry(configuration).Property(item => item.RowVersion).OriginalValue = rowVersion;
    }

    // Summary theo tên/trạng thái nguyên liệu, trước bộ lọc tồn và phân trang.
    public async Task<InventoryStockPage> GetStockAsync(
        int branchId,
        string? search,
        bool? isActive,
        string? status,
        int page,
        int pageSize)
    {
        IQueryable<InventoryStockRow> query =
            from ingredient in Context.Ingredients.AsNoTracking()
            join configuration in Context.BranchIngredients.Where(item => item.BranchId == branchId) on ingredient.Id equals configuration.IngredientId into configurations
            from configuration in configurations.DefaultIfEmpty()
            where !ingredient.Deleted && (search == null || ingredient.Name.Contains(search)) && (!isActive.HasValue || ingredient.IsActive == isActive.Value)
            select new InventoryStockRow

            {

                IngredientId = ingredient.Id,
                Name = ingredient.Name,
                UnitName = ingredient.Unit.Name,
                IsActive = ingredient.IsActive,
                ConfigurationId = configuration == null ? null : configuration.Id,
                Quantity = configuration == null ? null : configuration.Quantity,
                WarningThreshold = configuration == null ? null : configuration.WarningThreshold,
                UpdatedAt = configuration == null ? null : configuration.UpdatedAt,
                RowVersion = configuration == null ? null : configuration.RowVersion

            };
        InventoryStockPage result = new InventoryStockPage

        {

            ConfiguredCount = await query.CountAsync(item => item.ConfigurationId != null),
            UnconfiguredCount = await query.CountAsync(item => item.ConfigurationId == null),
            OutOfStockCount = await query.CountAsync(item => item.Quantity == 0),
            LowStockCount = await query.CountAsync(item => item.Quantity > 0 && item.WarningThreshold > 0 && item.Quantity <= item.WarningThreshold)

        };
        if (status == "UNCONFIGURED")
        {
            query = query.Where(item => item.ConfigurationId == null);
        }

        if (status == "OUT")
        {
            query = query.Where(item => item.Quantity == 0);
        }

        if (status == "LOW")
        {
            query = query.Where(item => item.Quantity > 0 && item.WarningThreshold > 0 && item.Quantity <= item.WarningThreshold);
        }

        if (status == "AVAILABLE")
        {
            query = query.Where(item => item.Quantity > 0 && (item.WarningThreshold == 0 || item.Quantity > item.WarningThreshold));
        }

        result.TotalCount = await query.CountAsync();
        result.Items = await query.OrderBy(item => item.Name).ThenBy(item => item.IngredientId).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return result;
    }
}
