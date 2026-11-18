namespace TuanKietBranchFlow.Infrastructure.Repositories.Inventory;

// Dữ liệu đọc phẳng cho phép lọc và phân trang tại SQL, không tải graph entity.
public class InventoryStockRow
{
    public int? ConfigurationId { get; set; }
    public int IngredientId { get; set; }
    public string Name { get; set; } = "";
    public string UnitName { get; set; } = "";
    public bool IsActive { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? WarningThreshold { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }
}
