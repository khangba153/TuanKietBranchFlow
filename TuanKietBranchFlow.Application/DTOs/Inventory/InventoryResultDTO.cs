namespace TuanKietBranchFlow.Application.DTOs.Inventory;

public class InventoryResultDTO
{
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int? SavedId { get; set; }
    public InventoryPageDTO? Inventory { get; set; }
}
