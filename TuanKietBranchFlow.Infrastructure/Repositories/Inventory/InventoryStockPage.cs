namespace TuanKietBranchFlow.Infrastructure.Repositories.Inventory;

public class InventoryStockPage
{
    public int ConfiguredCount { get; set; }
    public int OutOfStockCount { get; set; }
    public int LowStockCount { get; set; }
    public int UnconfiguredCount { get; set; }
    public int TotalCount { get; set; }
    public List<InventoryStockRow> Items { get; set; } = new List<InventoryStockRow>();
}
