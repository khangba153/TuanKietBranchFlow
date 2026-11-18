namespace TuanKietBranchFlow.Application.DTOs.Inventory;

public class InventoryThresholdRequestDTO
{
    public decimal WarningThreshold { get; set; }
    public byte[]? ExpectedRowVersion { get; set; }
}
