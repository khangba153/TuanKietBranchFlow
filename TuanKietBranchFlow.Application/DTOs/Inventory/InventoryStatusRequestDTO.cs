using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.Inventory;

public class InventoryStatusRequestDTO
{
    [Required]
    public bool? IsActive { get; set; }
}
