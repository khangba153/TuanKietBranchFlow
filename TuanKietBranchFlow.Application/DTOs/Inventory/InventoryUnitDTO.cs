namespace TuanKietBranchFlow.Application.DTOs.Inventory;

public class InventoryUnitDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
    public bool IsUsed { get; set; }
}
