using TuanKietBranchFlow.Application.DTOs.Auth;
using TuanKietBranchFlow.Web.Models;

namespace TuanKietBranchFlow.Web.Services;

// Tạo dữ liệu phiên Web từ kết quả đã được API xác thực
public static class WebAuthSessionFactory
{
    // Kiểm tra cặp token và hồ sơ trước khi tạo phiên
    public static WebAuthSession Create(
        LoginResponseDTO tokenResponse,
        CurrentUserDTO currentUser)
    {
        DateTime nowUtc = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken) ||
            string.IsNullOrWhiteSpace(tokenResponse.RefreshToken) ||
            tokenResponse.RefreshToken.Length != 44 ||
            tokenResponse.AccessTokenExpiresAt.Kind != DateTimeKind.Utc ||
            tokenResponse.RefreshTokenExpiresAt.Kind != DateTimeKind.Utc ||
            tokenResponse.AccessTokenExpiresAt <= nowUtc ||
            tokenResponse.RefreshTokenExpiresAt <= nowUtc ||
            tokenResponse.AccessTokenExpiresAt >
                tokenResponse.RefreshTokenExpiresAt)
        {
            throw new InvalidOperationException(
                "API trả dữ liệu token hoặc thời hạn không hợp lệ.");
        }

        if (currentUser.UserId <= 0 ||
            string.IsNullOrWhiteSpace(currentUser.Username) ||
            string.IsNullOrWhiteSpace(currentUser.Role))
        {
            throw new InvalidOperationException(
                "API trả hồ sơ người dùng không hợp lệ.");
        }

        return new WebAuthSession
        {
            Id = Guid.NewGuid(),
            UserId = currentUser.UserId,
            Username = currentUser.Username,
            Role = currentUser.Role,
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            AccessTokenExpiresAt = tokenResponse.AccessTokenExpiresAt,
            RefreshTokenExpiresAt = tokenResponse.RefreshTokenExpiresAt
        };
    }
}