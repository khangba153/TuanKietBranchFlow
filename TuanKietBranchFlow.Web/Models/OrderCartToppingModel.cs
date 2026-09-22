namespace TuanKietBranchFlow.Web.Models;

public class OrderCartToppingModel
{
    // Id thật sẽ được gửi cho API khi tạo đơn
    public int ToppingId { get; set; }

    // Dữ liệu dùng để hiển thị trong giỏ hàng
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal DisplayUnitPrice { get; set; }
}