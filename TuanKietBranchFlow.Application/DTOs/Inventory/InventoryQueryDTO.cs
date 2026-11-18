namespace TuanKietBranchFlow.Application.DTOs.Inventory;

public class InventoryQueryDTO
{
    public int BranchId { get; set; }
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
    public string? StockStatus { get; set; }
    public int Page { get; set; } = 1;
}
