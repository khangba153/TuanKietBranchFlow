using TuanKietBranchFlow.Application.DTOs.Orders.Management;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories.Branches;
using TuanKietBranchFlow.Infrastructure.Repositories.Orders;

namespace TuanKietBranchFlow.Application.Services.Orders;

public class OrderManagementService : IOrderManagementService
{
    private readonly IOrderRepository _orders;
    private readonly IBranchRepository _branches;

    public OrderManagementService(IOrderRepository orders, IBranchRepository branches)
    {
        _orders = orders;
        _branches = branches;
    }

    // Kiểm tra role, bộ lọc và phạm vi trước khi truy vấn đơn.
    public async Task<OrderManagementListResultDTO> GetOrdersAsync(
        int userId, string role, OrderManagementQueryDTO query)
    {
        OrderManagementListResultDTO result = new();
        if (userId <= 0 || (role != "ADMIN" && role != "OWNER"))
        {
            result.IsForbidden = true;
            return result;
        }

        string keyword = query.Keyword?.Trim() ?? string.Empty;
        string status = query.Status?.Trim().ToUpperInvariant() ?? string.Empty;
        if (query.Page < 1 || query.Page > 1000000 || query.PageSize < 1 || query.PageSize > 100 ||
            query.BranchId <= 0 || keyword.Length > 100 ||
            (status != "" && status != "COMPLETED" && status != "NEEDS_REVIEW" &&
             status != "ADJUSTED" && status != "CANCELLED"))
        {
            result.IsValid = false;
            result.ErrorMessage = "Bộ lọc hoặc thông tin phân trang không hợp lệ.";
            return result;
        }

        List<int> allowed = await GetBranchIdsAsync(userId, role);
        if (query.BranchId.HasValue)
        {
            // Chi nhánh cụ thể không tồn tại trả 404; có nhưng ngoài quyền trả 403.
            Branch? branch = await _branches.GetNotDeletedByIdAsync(query.BranchId.Value);
            if (branch == null) { result.IsBranchFound = false; return result; }
            if (!allowed.Contains(branch.Id)) { result.IsForbidden = true; return result; }
            allowed = new List<int> { branch.Id };
        }

        result.Data.Page = query.Page;
        result.Data.PageSize = query.PageSize;
        if (allowed.Count == 0) return result;

        OrderManagementPage page = await _orders.GetOrdersForManagementAsync(
            allowed, keyword, query.BusinessDate, status, (query.Page - 1) * query.PageSize, query.PageSize);
        result.Data.TotalCount = page.TotalCount;
        result.Data.Summary = new OrderManagementSummaryDTO
        {
            TotalOrders = page.Summary.TotalOrders,
            CompletedOrders = page.Summary.CompletedOrders,
            AdjustedOrders = page.Summary.AdjustedOrders,
            NeedsReviewOrders = page.Summary.NeedsReviewOrders,
            CancelledOrders = page.Summary.CancelledOrders,
            CompletedOrderValue = page.Summary.CompletedOrderValue
        };
        foreach (SalesOrder order in page.Items)
        {
            result.Data.Items.Add(new OrderManagementListItemDTO
            {
                Id = order.Id, Code = order.Code, BranchId = order.BranchId,
                BranchName = order.Branch.Name, CreatedByUserId = order.CreatedByUserId,
                CreatedByFullName = order.CreatedByUser.FullName, BusinessDate = order.BusinessDate,
                CreatedAt = order.CreatedAt, TotalAmount = order.TotalAmount,
                TotalQuantity = order.OrderItems.Sum(item => item.Quantity), Status = order.Status
            });
        }
        return result;
    }

    // Đơn không tồn tại hoặc ngoài phạm vi cùng trả null để không lộ dữ liệu.
    public async Task<OrderManagementDetailDTO?> GetDetailAsync(int userId, string role, int orderId)
    {
        if (userId <= 0 || orderId <= 0 || (role != "ADMIN" && role != "OWNER")) return null;
        List<int> allowed = await GetBranchIdsAsync(userId, role);
        if (allowed.Count == 0) return null;
        SalesOrder? order = await _orders.GetOrderDetailForManagementAsync(orderId, allowed);
        if (order == null || !allowed.Contains(order.BranchId)) return null;

        OrderManagementDetailDTO detail = new()
        {
            Id = order.Id, Code = order.Code, BranchId = order.BranchId,
            BranchCode = order.Branch.Code, BranchName = order.Branch.Name,
            CreatedByUserId = order.CreatedByUserId, CreatedByFullName = order.CreatedByUser.FullName,
            BusinessDate = order.BusinessDate, CreatedAt = order.CreatedAt,
            TotalAmount = order.TotalAmount, Status = order.Status,
            TotalQuantity = order.OrderItems.Sum(item => item.Quantity), ReportReason = order.ReportReason,
            ReportedAt = order.ReportedAt, ReportedByFullName = order.ReportedByUser?.FullName
        };
        // Luôn dùng snapshot đã lưu, không đọc lại tên/giá hiện tại từ menu.
        foreach (OrderItem item in order.OrderItems.OrderBy(item => item.Id))
        {
            OrderManagementItemDTO row = new()
            {
                Id = item.Id, ProductName = item.ProductNameSnapshot, SizeName = item.SizeNameSnapshot,
                Quantity = item.Quantity, UnitPrice = item.UnitPriceSnapshot, SubtotalAmount = item.SubtotalAmount
            };
            foreach (OrderItemTopping topping in item.OrderItemToppings)
                row.Toppings.Add(new OrderManagementToppingDTO
                { Name = topping.ToppingNameSnapshot, Quantity = topping.Quantity, UnitPrice = topping.UnitPriceSnapshot });
            foreach (OrderItemNote note in item.OrderItemNotes) row.Notes.Add(note.NoteNameSnapshot);
            detail.Items.Add(row);
        }
        return detail;
    }

    // OWNER đọc tất cả chi nhánh chưa xóa; ADMIN chỉ phân công đang có hiệu lực.
    private async Task<List<int>> GetBranchIdsAsync(int userId, string role)
    {
        List<Branch> branches = role == "OWNER"
            ? await _branches.GetAllNotDeletedAsync()
            : await _branches.GetAssignedBranchesAsync(userId, DateOnly.FromDateTime(DateTime.Today));
        return branches.Select(branch => branch.Id).Distinct().ToList();
    }
}
