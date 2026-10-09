using TuanKietBranchFlow.Application.DTOs.Auth;
using TuanKietBranchFlow.Web.Models;

namespace TuanKietBranchFlow.Web.Services;

// Điều phối đăng nhập API, lưu phiên RAM và phát cookie Web
public class WebLoginService
{
    private readonly AuthApiService _authApiService;
    private readonly IWebAuthSessionStore _sessionStore;
    private readonly WebCookieSignInService _cookieSignInService;

    public WebLoginService(
        AuthApiService authApiService,
        IWebAuthSessionStore sessionStore,
        WebCookieSignInService cookieSignInService)
    {
        _authApiService = authApiService;
        _sessionStore = sessionStore;
        _cookieSignInService = cookieSignInService;
    }

    // Chỉ hoàn tất đăng nhập khi đã lưu phiên và phát cookie thành công
    public async Task<bool> LoginAsync(
        HttpContext httpContext,
        LoginRequestDTO request)
    {
        if (httpContext.Response.HasStarted)
        {
            throw new InvalidOperationException(
                "Không thể đăng nhập sau khi HTTP response đã bắt đầu.");
        }

        LoginResponseDTO? tokenResponse =
            await _authApiService.LoginAsync(request);

        if (tokenResponse == null)
        {
            return false;
        }

        CurrentUserDTO? currentUser =
            await _authApiService.GetCurrentUserByAccessTokenAsync(
                tokenResponse.AccessToken);

        if (currentUser == null)
        {
            return false;
        }

        WebAuthSession session =
            WebAuthSessionFactory.Create(tokenResponse, currentUser);

        if (!_sessionStore.TryAdd(session))
        {
            throw new InvalidOperationException(
                "Không thể thêm phiên đăng nhập vào kho Web.");
        }

        try
        {
            await _cookieSignInService.SignInAsync(
                httpContext,
                session);
        }
        catch
        {
            // Không giữ phiên RAM nếu bước phát cookie gặp lỗi
            _sessionStore.Remove(session.Id);
            throw;
        }

        return true;
    }
}