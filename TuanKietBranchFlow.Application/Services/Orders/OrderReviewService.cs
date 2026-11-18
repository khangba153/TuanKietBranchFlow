using TuanKietBranchFlow.Application.DTOs.Orders.Management;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories.Orders;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;

namespace TuanKietBranchFlow.Application.Services.Orders;

public class OrderReviewService : IOrderReviewService
{
    private readonly IOrderManagementService _orderManagementService;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _work;
    public OrderReviewService(
        IOrderManagementService management,
        IOrderRepository orders,
        IUnitOfWork work)
    {
        _orderManagementService = management;
        _orderRepository = orders;
        _work = work;
    }

    // Giữ nguyên/hủy đơn và ghi lịch sử là một transaction, không sửa món/giá hoặc báo sai gốc.
    public async Task<OrderReviewResultDTO> ResolveAsync(
        int userId,
        string role,
        int orderId,
        ResolveOrderReviewRequestDTO request)
    {
        if (userId <= 0 || role != "ADMIN")
        {
            return new OrderReviewResultDTO()

            {

                IsForbidden = true

            };
        }

        string reason = request.Reason?.Trim() ?? "";
        if (orderId <= 0 || (request.Action != "KEEP" && request.Action != "CANCEL") || reason.Length == 0 || reason.Length > 500 || request.ExpectedReportedAt == null || request.ExpectedHistoryId < 0)
        {
            return new OrderReviewResultDTO()

            {

                IsInvalid = true

            };
        }

        OrderManagementDetailDTO? detail = await _orderManagementService.GetDetailAsync(userId, role, orderId);
        if (detail == null)
        {
            return new OrderReviewResultDTO()

            {

                IsNotFound = true

            };
        }

        if (detail.Status != "NEEDS_REVIEW" || detail.ReportedAt != request.ExpectedReportedAt || detail.LatestHistoryId != request.ExpectedHistoryId)
        {
            return new OrderReviewResultDTO()

            {

                IsConflict = true

            };
        }

        string beforeData = OrderSnapshot.Serialize(detail);
        string targetStatus = request.Action == "KEEP" ? "COMPLETED" : "CANCELLED";
        DateTime now = DateTime.UtcNow;
        await _work.BeginTransactionAsync();
        try
        {
            int changed = await _orderRepository.TryResolveReviewAsync(
                orderId,
                userId,
                DateOnly.FromDateTime(DateTime.Today),
                request.ExpectedReportedAt.Value,
                request.ExpectedHistoryId,
                targetStatus,
                now);
            if (changed != 1)
            {
                await _work.RollbackTransactionAsync();
                return new OrderReviewResultDTO()

                {

                    IsConflict = true

                };
            }

            detail.Status = targetStatus;
            await _orderRepository.AddReviewHistoryAsync(new OrderAdjustment
            {
                SalesOrderId = orderId,
                AdjustedByUserId = userId,
                Reason = reason,
                BeforeData = beforeData,
                AfterData = OrderSnapshot.Serialize(detail),
                CreatedAt = now
            });
            await _work.SaveChangesAsync();
            await _work.CommitTransactionAsync();
            return new OrderReviewResultDTO()

            {

                Status = targetStatus

            };
        }
        catch
        {
            await _work.RollbackTransactionAsync();
            _work.ClearTracking();
            throw;
        }
    }
}
