namespace TuanKietBranchFlow.Web.Services;

// Quản lý việc lấy token để Web server gọi API
public interface IWebTokenService
{
    // Lấy access token còn hạn, refresh nếu cần; null khi phiên không hợp lệ
    Task<string?> GetAccessTokenAsync(Guid webSessionId);
}