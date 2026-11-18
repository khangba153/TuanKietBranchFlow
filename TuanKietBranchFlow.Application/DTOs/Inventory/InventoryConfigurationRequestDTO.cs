namespace TuanKietBranchFlow.Application.DTOs.Inventory;

public class InventoryConfigurationRequestDTO
{
    public int BranchId { get; set; }
    public int IngredientId { get; set; }
    public decimal WarningThreshold { get; set; }
}
