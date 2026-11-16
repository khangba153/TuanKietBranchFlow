using TuanKietBranchFlow.Application.DTOs.Orders.Management;

namespace TuanKietBranchFlow.Application.Services.Orders;

public interface IOrderReviewService
{
    Task<OrderReviewResultDTO> ResolveAsync(int userId, string role, int orderId, ResolveOrderReviewRequestDTO request);
}
