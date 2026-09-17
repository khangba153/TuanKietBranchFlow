namespace TuanKietBranchFlow.Application.DTOs.OrderMenus;

public class OrderMenuProductSizeDTO
{
    // Id của lựa chọn ProductSize sẽ được giữ trong giỏ hàng
    public int Id { get; set; }
    public int SizeId { get ; set; }
    public string SizeName { get; set; } = string.Empty;
    public decimal Price { get; set; }
}