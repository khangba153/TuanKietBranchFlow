namespace TuanKietBranchFlow.Application.DTOs.Orders.Management;

// Kết quả nghiệp vụ nội bộ; controller chỉ trả Page khi thành công.
public class OrderManagementListResultDTO
{
    public bool IsValid { get; set; } = true;
    public bool IsForbidden { get; set; }
    public bool IsBranchFound { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public OrderManagementPageDTO Data { get; set; } = new OrderManagementPageDTO();
}
