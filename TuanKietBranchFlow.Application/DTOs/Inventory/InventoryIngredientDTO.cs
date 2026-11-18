namespace TuanKietBranchFlow.Application.DTOs.Inventory;

public class InventoryIngredientDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int UnitId { get; set; }
    public string UnitName { get; set; } = "";
    public bool IsActive { get; set; }
    public bool IsUsed { get; set; }
}
