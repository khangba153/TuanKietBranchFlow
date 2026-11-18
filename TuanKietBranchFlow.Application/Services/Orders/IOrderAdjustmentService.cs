using TuanKietBranchFlow.Application.DTOs.Orders.Management;

namespace TuanKietBranchFlow.Application.Services.Orders;

public interface IOrderAdjustmentService
{
    Task<OrderAdjustmentResultDTO> GetOptionsAsync(
        int userId,
        string role,
        int orderId);
    Task<OrderAdjustmentResultDTO> PreviewAsync(
        int userId,
        string role,
        int orderId,
        AdjustOrderRequestDTO request);
    Task<OrderAdjustmentResultDTO> AdjustAsync(
        int userId,
        string role,
        int orderId,
        AdjustOrderRequestDTO request);
}
