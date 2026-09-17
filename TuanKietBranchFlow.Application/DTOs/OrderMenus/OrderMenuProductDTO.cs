namespace TuanKietBranchFlow.Application.DTOs.OrderMenus;

public class OrderMenuProductDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    // Giá nhỏ nhất dùng để hiển thị tại màn hình danh sách món
    public decimal MinPrice { get; set; }

    // Các lựa chọn size được dùng khi employee mở chi tiết món
    public List<OrderMenuProductSizeDTO> ProductSizes { get; set; }
        = new List<OrderMenuProductSizeDTO>();
}