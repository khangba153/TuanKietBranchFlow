namespace TuanKietBranchFlow.Web.Models;

// Dữ liệu của một phiên đăng nhập, chỉ sử dụng bên trong Web server
public class WebAuthSession
{
    // Mã phiên riêng của Web, không phải sid trong JWT của API
    public Guid Id { get; set; }

    // Thông tin tài khoản đã được API xác thực
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    // Token API do Web server giữ, không gửi vào localStorage
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;

    // Thời điểm hết hạn theo UTC, lấy từ response của API
    public DateTime AccessTokenExpiresAt { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }

    // Đồng bộ thao tác với token của riêng phiên này
    public SemaphoreSlim TokenLock { get; } = new SemaphoreSlim(1, 1);
}