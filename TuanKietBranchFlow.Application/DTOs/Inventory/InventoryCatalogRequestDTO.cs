using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.Inventory;

public class InventoryCatalogRequestDTO
{
    [Required]
    public string Name { get; set; } = "";
    public int UnitId { get; set; }
}
