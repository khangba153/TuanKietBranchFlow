using TuanKietBranchFlow.Application.DTOs.Inventory;

namespace TuanKietBranchFlow.Application.Services.Inventory;

public interface IInventoryService
{
    Task<List<InventoryUnitDTO>> GetUnitsAsync(string? search);
    Task<List<InventoryIngredientDTO>> GetIngredientsAsync(string? search);
    Task<InventoryResultDTO> CreateUnitAsync(string name);
    Task<InventoryResultDTO> UpdateUnitAsync(int unitId, string name);
    Task<InventoryResultDTO> UpdateUnitStatusAsync(int unitId, bool isActive);
    Task<InventoryResultDTO> CreateIngredientAsync(string name, int unitId);
    Task<InventoryResultDTO> UpdateIngredientAsync(int ingredientId, string name, int unitId);
    Task<InventoryResultDTO> UpdateIngredientStatusAsync(int ingredientId, bool isActive);
    Task<InventoryResultDTO> GetStockAsync(
        int userId,
        string role,
        InventoryQueryDTO query);
    Task<InventoryResultDTO> ConfigureAsync(
        int userId,
        InventoryConfigurationRequestDTO request);
    Task<InventoryResultDTO> UpdateThresholdAsync(
        int userId,
        int id,
        InventoryThresholdRequestDTO request);
}
