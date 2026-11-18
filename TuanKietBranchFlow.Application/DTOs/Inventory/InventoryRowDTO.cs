namespace TuanKietBranchFlow.Application.DTOs.Inventory;

public class InventoryRowDTO
{
    public int? ConfigurationId { get; set; }
    public int IngredientId { get; set; }
    public string Name { get; set; } = "";
    public string UnitName { get; set; } = "";
    public bool IsActive { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? WarningThreshold { get; set; }
    public string StockStatus { get; set; } = "";
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }
}
