namespace TuanKietBranchFlow.Application.DTOs.Orders;

public class CreateOrderResponseDTO
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public DateOnly BusinessDate { get; set; }

    // Tổng tiền do Backend đọc giá từ db và tính lại
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}