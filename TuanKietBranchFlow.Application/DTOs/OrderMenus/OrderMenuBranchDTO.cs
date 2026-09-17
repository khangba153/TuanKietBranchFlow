namespace TuanKietBranchFlow.Application.DTOs.OrderMenus;

public class OrderMenuBranchDTO
{
    // Thông tin chi nhánh sở hữu menu đang được trả về
    public int Id { get ; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}