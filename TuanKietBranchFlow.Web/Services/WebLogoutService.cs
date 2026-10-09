using System.Security.Claims;
using TuanKietBranchFlow.Web.Models;

namespace TuanKietBranchFlow.Web.Services;

// Phối hợp thu hồi phiên API, dọn RAM và xóa cookie Web.
public class WebLogoutService
{
    private readonly AuthApiService _authApiService;
    private readonly IWebAuthSessionStore _sessionStore;
    private readonly IWebTokenService _webTokenService;
    private readonly WebCookieSignInService _cookieSignInService;

    public WebLogoutService(
        AuthApiService authApiService,
        IWebAuthSessionStore sessionStore,
        IWebTokenService webTokenService,
        WebCookieSignInService cookieSignInService)
    {
        _authApiService = authApiService;
        _sessionStore = sessionStore;
        _webTokenService = webTokenService;
        _cookieSignInService = cookieSignInService;
    }

    // Trả true chỉ khi API đã xác nhận thu hồi tất cả phiên.
    public async Task<bool> LogoutAsync(HttpContext httpContext)
    {
        if (httpContext.Response.HasStarted)
        {
            throw new InvalidOperationException(
                "Không thể đăng xuất sau khi HTTP response đã bắt đầu.");
        }

        bool isApiLogoutConfirmed = false;

        ClaimsPrincipal user = httpContext.User;

        string? sessionIdValue = user.FindFirstValue(
            WebCookieAuthenticationEvents.WebSessionIdClaim);

        string? userIdValue = user.FindFirstValue(
            ClaimTypes.NameIdentifier);

        // Chỉ gọi API khi danh tính cookie có thông tin phiên hợp lệ.
        if (user.Identity?.IsAuthenticated == true &&
            Guid.TryParse(sessionIdValue, out Guid sessionId) &&
            sessionId != Guid.Empty &&
            int.TryParse(userIdValue, out int userId) &&
            userId > 0)
        {
            WebAuthSession? session =
                _sessionStore.GetById(sessionId);

            // Không sử dụng phiên RAM thuộc tài khoản khác.
            if (session != null && session.UserId == userId)
            {
                List<Guid> sessionIds =
                    _sessionStore.GetSessionIdsByUserId(userId);

                string? accessToken =
                    await _webTokenService.GetAccessTokenAsync(sessionId);

                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    isApiLogoutConfirmed =
                        await _authApiService.LogoutByAccessTokenAsync(
                            accessToken);
                }

                // Chỉ dọn các phiên cùng tài khoản khi API xác nhận.
                if (isApiLogoutConfirmed)
                {
                    foreach (Guid id in sessionIds)
                    {
                        _sessionStore.Remove(id);
                    }
                }

                // Phiên hiện tại kết thúc kể cả khi API trả 401.
                _sessionStore.Remove(sessionId);
            }
        }

        await _cookieSignInService.SignOutAsync(httpContext);

        return isApiLogoutConfirmed;
    }
}