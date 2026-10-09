using TuanKietBranchFlow.Web.Models;

namespace TuanKietBranchFlow.Web.Services;

// Khai báo các thao tác quản lý phiên trong RAM của Web server
public interface IWebAuthSessionStore
{
    // Trả false nếu phiên không hợp lệ, trùng mã hoặc kho đã đầy
    bool TryAdd(WebAuthSession session);

    // Tìm phiên theo mã riêng của Web
    WebAuthSession? GetById(Guid sessionId);

    // Xóa dữ liệu phiên khỏi kho RAM của Web
    void Remove(Guid sessionId);

    // Dọn phiên hết hạn và trả số phiên đã xóa khỏi RAM
    int RemoveExpiredSessions();

    // Lấy mã các phiên Web đang được lưu của cùng tài khoản.
    List<Guid> GetSessionIdsByUserId(int userId);
}