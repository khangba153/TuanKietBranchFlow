using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using TuanKietBranchFlow.Web.Models;

namespace TuanKietBranchFlow.Web.Services;

// Phát cookie cho phiên đã được tạo trên Web server
public class WebCookieSignInService
{
    private readonly IWebAuthSessionStore _sessionStore;

    public WebCookieSignInService(
        IWebAuthSessionStore sessionStore)
    {
        _sessionStore = sessionStore;
    }

    // Tạo danh tính và cookie từ phiên đã lưu trong RAM
    public async Task SignInAsync(
        HttpContext httpContext,
        WebAuthSession session)
    {
        if (_sessionStore.GetById(session.Id) != session)
        {
            throw new InvalidOperationException(
                "Phiên Web chưa được lưu hoặc đã hết hạn.");
        }

        List<Claim> claims = new List<Claim>
        {
            new Claim(
                WebCookieAuthenticationEvents.WebSessionIdClaim,
                session.Id.ToString()),

            new Claim(
                ClaimTypes.NameIdentifier,
                session.UserId.ToString()),

            new Claim(ClaimTypes.Name, session.Username),
            new Claim(ClaimTypes.Role, session.Role)
        };

        ClaimsIdentity identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        ClaimsPrincipal principal = new ClaimsPrincipal(identity);

        AuthenticationProperties properties =
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = false,
                ExpiresUtc = new DateTimeOffset(
                    session.RefreshTokenExpiresAt)
            };

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties);
    }

    // Xóa cookie đăng nhập thông qua HTTP response của Web.
    public async Task SignOutAsync(HttpContext httpContext)
    {
        if (httpContext.Response.HasStarted)
        {
            throw new InvalidOperationException(
                "Không thể xóa cookie khi HTTP response đã bắt đầu.");
        }

        await httpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
    }
}