using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories.Common;

namespace TuanKietBranchFlow.Infrastructure.Repositories.Inventory;

public interface IInventoryRepository : IRepositoryBase<BranchIngredient>
{
    Task LockConfigurationAsync();
    Task<List<IngredientUnit>> GetUnitsAsync(string? search);
    Task<List<Ingredient>> GetIngredientsAsync(string? search);
    Task<IngredientUnit?> GetUnitAsync(int id);
    Task<Ingredient?> GetIngredientAsync(int id);
    Task<bool> UnitNameExistsAsync(
        string name,
        int exceptId);
    Task<bool> IngredientNameExistsAsync(
        string name,
        int exceptId);
    Task<bool> UnitIsUsedAsync(int id);
    Task<bool> UnitHasActiveIngredientsAsync(int id);
    Task<bool> IngredientIsUsedAsync(int id);
    Task<List<int>> GetUsedUnitIdsAsync();
    Task<List<int>> GetUsedIngredientIdsAsync();
    Task AddUnitAsync(IngredientUnit unit);
    Task AddIngredientAsync(Ingredient ingredient);
    Task<BranchIngredient?> GetConfigurationAsync(int id);
    Task<bool> ConfigurationExistsAsync(
        int branchId,
        int ingredientId);
    void SetExpectedRowVersion(
        BranchIngredient configuration,
        byte[] rowVersion);
    Task<InventoryStockPage> GetStockAsync(
        int branchId,
        string? search,
        bool? isActive,
        string? status,
        int page,
        int pageSize);
}
