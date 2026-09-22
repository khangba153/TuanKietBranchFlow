namespace TuanKietBranchFlow.Web.Models;

public class OrderCartItemModel
{
    // ProductSize.Id xác định đúng món, size và giá tương ứng
    public int ProductSizeId { get; set; }

    // Dữ liệu dùng để hiển thị trong giỏ hàng
    public string ProductName { get; set; } = string.Empty;
    public string SizeName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal DisplayProductSizePrice { get; set; }
    public decimal DisplaySubtotal { get; set; }

    public List<OrderCartToppingModel> Toppings { get; set; } =
        new List<OrderCartToppingModel>();

    public List<OrderCartNoteModel> Notes { get; set; } =
        new List<OrderCartNoteModel>();
}