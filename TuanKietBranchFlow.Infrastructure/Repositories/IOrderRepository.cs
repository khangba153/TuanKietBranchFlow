using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public interface IOrderRepository : IRepositoryBase<SalesOrder>
{
    // Lấy các món-size hợp lệ và đang bán tại chi nhánh
    Task<List<ProductSize>> GetAvailableProductSizesAsync(
        int branchId, List<int> productSizeIds);

    // Lấy các topping hợp lệ và đang bán tại chi nhánh
    Task<List<Topping>> GetAvailableToppingsAsync(
        int branchId, List<int> toppingIds);

    // Ghi chú dùng chung, không phụ thuộc chi nhánh
    Task<List<NoteOption>> GetActiveNoteOptionsAsync(
        List<int> noteOptionIds);

    // Sinh mã đơn và số thứ tự an toàn theo chi nhánh và ngày nghiệp vụ
    Task<OrderCodeResult> GetNextOrderCodeAsync(
        int branchId, DateOnly businessDate);

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
}