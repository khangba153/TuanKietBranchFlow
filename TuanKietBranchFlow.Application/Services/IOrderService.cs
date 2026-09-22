using TuanKietBranchFlow.Application.DTOs.Orders;

namespace TuanKietBranchFlow.Application.Services;

public interface IOrderService
{
    // Kiểm tra nghiệp vụ, tính giá và tạo đơn cho employee hiện tại
    Task<CreateOrderResultDTO> CreateOrderAsync(
        int currentUserId,
        CreateOrderRequestDTO request);

    // Lấy lịch sử đơn do employee hiện tại tạo
    Task<List<MyOrderListItemDTO>> GetMyOrdersAsync(
        int currentUserId,
        string keyword,
        DateOnly? businessDate);

    // Lấy chi tiết 1 đơn do employee hiện tại tạo
    Task<MyOrderDetailDTO?> GetMyOrderDetailAsync(
        int currentUserId,
        int orderId);

    // Báo sai đơn do chính employee hiện tại tạo
    Task<ReportOrderResult> ReportOrderAsync(
        int currentUserId,
        int orderId,
        ReportOrderRequestDTO request);
}