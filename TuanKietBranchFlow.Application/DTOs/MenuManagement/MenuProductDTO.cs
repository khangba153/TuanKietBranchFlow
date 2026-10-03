namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class MenuProductDTO
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }

    public List<MenuProductSizeDTO> ProductSizes { get; set; } = new();
}