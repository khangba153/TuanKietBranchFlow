namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class MenuToppingDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}