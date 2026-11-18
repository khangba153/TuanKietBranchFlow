using TuanKietBranchFlow.Application.DTOs.Orders.Management;

namespace TuanKietBranchFlow.Application.Services.Orders;

public interface IOrderManagementService
{
    Task<OrderManagementListResultDTO> GetOrdersAsync(
        int userId,
        string role,
        OrderManagementQueryDTO query);
    Task<OrderManagementDetailDTO?> GetDetailAsync(
        int userId,
        string role,
        int orderId);
}
