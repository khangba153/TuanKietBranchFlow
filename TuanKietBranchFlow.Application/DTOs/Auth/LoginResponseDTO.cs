namespace TuanKietBranchFlow.Application.DTOs.Auth;

public class LoginResponseDTO
{
    // Token dùng để gọi các API cần đăng nhập
    public string AccessToken { get; set; } = string.Empty;

    // Token dùng để xin cặp token mới
    public string RefreshToken { get; set; } = string.Empty;

    // Thời điểm access token hết hạn, theo UTC
    public DateTime AccessTokenExpiresAt { get; set; }

    // Thời điểm refresh token hết hạn, theo UTC
    public DateTime RefreshTokenExpiresAt { get; set; }
}