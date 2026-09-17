namespace TuanKietBranchFlow.Application.DTOs.OrderMenus;

public class OrderMenuCategoryDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Chỉ chứa những món còn hoạt động và đang bán tại chi nhánh
    public List<OrderMenuProductDTO> Products { get; set; }
        = new List<OrderMenuProductDTO>();
}