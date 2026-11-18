using TuanKietBranchFlow.Infrastructure.Repositories.Common;
using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories.Orders;

public interface IOrderRepository : IRepositoryBase<SalesOrder>
{
    Task<bool> LockAndCheckEmployeeBranchAsync(
        int userId,
        int branchId,
        DateOnly today);
    // Cập nhật có điều kiện trong transaction do service quản lý; 0 nghĩa là dữ liệu/quyền đã đổi.
    Task<int> TryResolveReviewAsync(
        int orderId,
        int adminId,
        DateOnly assignmentDate,
        DateTime expectedReportedAt,
        int expectedHistoryId,
        string status,
        DateTime updatedAt);
    Task AddReviewHistoryAsync(OrderAdjustment history);
    // Chỉ gọi sau UPDATE giành quyền xử lý trong cùng transaction.
    Task ApplyAdjustedItemsAsync(
        int orderId,
        List<OrderItem> items,
        decimal totalAmount);
    // Chỉ đọc các đơn nằm trong tập chi nhánh Service đã cho phép.
    Task<OrderManagementPage> GetOrdersForManagementAsync(
        List<int> branchIds,
        string keyword,
        DateOnly? businessDate,
        string status,
        int skip,
        int take);
    // Lọc phạm vi ngay trong SQL, không đọc đơn ngoài quyền rồi mới trả về.
    Task<SalesOrder?> GetOrderDetailForManagementAsync(
        int orderId,
        List<int> branchIds);
    // Lấy các món-size hợp lệ và đang bán tại chi nhánh
    Task<List<ProductSize>> GetAvailableProductSizesAsync(
        int branchId,
        List<int> productSizeIds);
    // Lấy các topping hợp lệ và đang bán tại chi nhánh
    Task<List<Topping>> GetAvailableToppingsAsync(
        int branchId,
        List<int> toppingIds);
    // Ghi chú dùng chung, không phụ thuộc chi nhánh
    Task<List<NoteOption>> GetActiveNoteOptionsAsync(List<int> noteOptionIds);
    // Sinh mã đơn và số thứ tự an toàn theo chi nhánh và ngày nghiệp vụ
    Task<OrderCodeResult> GetNextOrderCodeAsync(
        int branchId,
        DateOnly businessDate);
    // Lấy các đơn do người dùng hiện tại tạo, kèm dữ liệu để tính tổng số món
    Task<List<SalesOrder>> GetOrdersCreatedByUserAsync(
        int currentUserId,
        string keyword,
        DateOnly? businessDate);
    // Lấy chi tiết một đơn do chính employee hiện tại tạo
    Task<SalesOrder?> GetOrderDetailCreatedByUserAsync(
        int orderId,
        int currentUserId);
    // Lấy đơn thuộc chính employee để cập nhật trạng thái báo sai
    Task<SalesOrder?> GetOrderForReportAsync(
        int orderId,
        int currentUserId);
    // Cập nhật nguyên tử; chỉ nhận báo sai trên đúng phiên bản employee đã xem.
    Task<int> TryReportOrderAsync(
        int orderId,
        int currentUserId,
        int expectedHistoryId,
        string reason,
        DateTime reportedAt);
}
