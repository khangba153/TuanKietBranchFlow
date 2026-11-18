namespace TuanKietBranchFlow.Application.DTOs.Inventory;

public class InventoryPageDTO
{
    public bool CanConfigure { get; set; }
    public int ConfiguredCount { get; set; }
    public int OutOfStockCount { get; set; }
    public int LowStockCount { get; set; }
    public int UnconfiguredCount { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; } = 20;
    public List<InventoryRowDTO> Items { get; set; } = new List<InventoryRowDTO>();
}
