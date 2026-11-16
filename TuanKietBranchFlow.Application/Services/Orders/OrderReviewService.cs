using System.Text.Json;
using TuanKietBranchFlow.Application.DTOs.Orders.Management;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories.Orders;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;

namespace TuanKietBranchFlow.Application.Services.Orders;

public class OrderReviewService : IOrderReviewService
{
    private readonly IOrderManagementService _management;
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _work;

    public OrderReviewService(IOrderManagementService management, IOrderRepository orders, IUnitOfWork work)
    { _management = management; _orders = orders; _work = work; }

    // Giữ nguyên/hủy đơn và ghi lịch sử là một transaction, không sửa món/giá hoặc báo sai gốc.
    public async Task<OrderReviewResultDTO> ResolveAsync(int userId, string role, int orderId, ResolveOrderReviewRequestDTO request)
    {
        if (userId <= 0 || role != "ADMIN") return new() { IsForbidden = true };
        string reason = request.Reason?.Trim() ?? "";
        if (orderId <= 0 || (request.Action != "KEEP" && request.Action != "CANCEL") ||
            reason.Length == 0 || reason.Length > 500 || request.ExpectedReportedAt == null || request.ExpectedHistoryId < 0)
            return new() { IsInvalid = true };

        OrderManagementDetailDTO? detail = await _management.GetDetailAsync(userId, role, orderId);
        if (detail == null) return new() { IsNotFound = true };
        if (detail.Status != "NEEDS_REVIEW" || detail.ReportedAt != request.ExpectedReportedAt ||
            detail.LatestHistoryId != request.ExpectedHistoryId) return new() { IsConflict = true };

        string beforeData = SerializeSnapshot(detail);
        string targetStatus = request.Action == "KEEP" ? "COMPLETED" : "CANCELLED";
        DateTime now = DateTime.UtcNow;
        await _work.BeginTransactionAsync();
        try
        {
            int changed = await _orders.TryResolveReviewAsync(orderId, userId, DateOnly.FromDateTime(DateTime.Today),
                request.ExpectedReportedAt.Value, request.ExpectedHistoryId, targetStatus, now);
            if (changed != 1)
            {
                await _work.RollbackTransactionAsync();
                return new() { IsConflict = true };
            }
            detail.Status = targetStatus;
            await _orders.AddReviewHistoryAsync(new OrderAdjustment
            {
                SalesOrderId = orderId, AdjustedByUserId = userId, Reason = reason,
                BeforeData = beforeData, AfterData = SerializeSnapshot(detail), CreatedAt = now
            });
            await _work.SaveChangesAsync();
            await _work.CommitTransactionAsync();
            return new() { Status = targetStatus };
        }
        catch
        {
            await _work.RollbackTransactionAsync();
            _work.ClearTracking();
            throw;
        }
    }

    // Chỉ lưu trạng thái nghiệp vụ và snapshot món; không lồng toàn bộ lịch sử cũ vào lịch sử mới.
    private static string SerializeSnapshot(OrderManagementDetailDTO detail) => JsonSerializer.Serialize(new
    {
        detail.Id, detail.Code, detail.BranchId, detail.CreatedByUserId,
        detail.BusinessDate, detail.CreatedAt, detail.Status, detail.TotalAmount,
        detail.ReportReason, detail.ReportedAt, detail.ReportedByUserId, detail.ReportedByFullName, detail.Items
    });
}
