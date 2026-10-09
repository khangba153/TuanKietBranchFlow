using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using TuanKietBranchFlow.Web.Models;

namespace TuanKietBranchFlow.Web.Services;

// Kiểm tra cookie còn gắn với một phiên hợp lệ trong kho RAM
public class WebCookieAuthenticationEvents : CookieAuthenticationEvents
{
    public const string WebSessionIdClaim = "web_session_id";

    private readonly IWebAuthSessionStore _sessionStore;

    public WebCookieAuthenticationEvents(
        IWebAuthSessionStore sessionStore)
    {
        _sessionStore = sessionStore;
    }

    // Chỉ chấp nhận cookie có mã phiên và tài khoản khớp kho RAM
    public override async Task ValidatePrincipal(
        CookieValidatePrincipalContext context)
    {
        string? sessionIdValue =
            context.Principal?.FindFirstValue(WebSessionIdClaim);

        string? userIdValue =
            context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(sessionIdValue, out Guid sessionId) ||
            sessionId == Guid.Empty ||
            !int.TryParse(userIdValue, out int userId) ||
            userId <= 0)
        {
            context.RejectPrincipal();

            await context.HttpContext.SignOutAsync(
                context.Scheme.Name);

            return;
        }

        WebAuthSession? session =
            _sessionStore.GetById(sessionId);

        if (session == null || session.UserId != userId)
        {
            context.RejectPrincipal();

            await context.HttpContext.SignOutAsync(
                context.Scheme.Name);
        }
    }
}