namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class MenuToppingGroupDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public List<MenuToppingDTO> Toppings { get; set; } =
        new List<MenuToppingDTO>();
}