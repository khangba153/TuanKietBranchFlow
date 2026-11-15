using TuanKietBranchFlow.Application.DTOs.OrderMenus;

namespace TuanKietBranchFlow.Application.Services.Orders;

public interface IOrderMenuService
{
    // Lấy menu gọi món sau khi kiểm tra chi nhánh và phân công
    Task<OrderMenuResultDTO> GetOrderMenuAsync(
        int currentUserId,
        int branchId);
}