using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using TuanKietBranchFlow.Web.Models;

namespace TuanKietBranchFlow.Web.Services;

// Nhận danh tính từ host và kiểm tra lại phiên trong Blazor circuit.
public class WebAuthenticationStateProvider
    : RevalidatingServerAuthenticationStateProvider
{
    private readonly IWebAuthSessionStore _sessionStore;

    public WebAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        IWebAuthSessionStore sessionStore)
        : base(loggerFactory)
    {
        _sessionStore = sessionStore;
    }

    // Kiểm tra lại phiên của circuit mỗi phút.
    protected override TimeSpan RevalidationInterval
    {
        get
        {
            return TimeSpan.FromMinutes(1);
        }
    }

    // Chỉ giữ trạng thái đăng nhập nếu danh tính khớp phiên RAM.
    protected override Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ClaimsPrincipal user = authenticationState.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            return Task.FromResult(false);
        }

        string? sessionIdValue = user.FindFirstValue(
            WebCookieAuthenticationEvents.WebSessionIdClaim);

        string? userIdValue = user.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(sessionIdValue, out Guid sessionId) ||
            sessionId == Guid.Empty ||
            !int.TryParse(userIdValue, out int userId) ||
            userId <= 0)
        {
            return Task.FromResult(false);
        }

        WebAuthSession? session =
            _sessionStore.GetById(sessionId);

        if (session == null || session.UserId != userId)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    // Bỏ phiên khỏi RAM và cập nhật circuit nếu đang sử dụng đúng phiên đó.
    public async Task RejectSessionAsync(Guid webSessionId)
    {
        if (webSessionId == Guid.Empty)
        {
            return;
        }

        _sessionStore.Remove(webSessionId);

        AuthenticationState authenticationState =
            await GetAuthenticationStateAsync();

        string? currentSessionIdValue =
            authenticationState.User.FindFirstValue(
                WebCookieAuthenticationEvents.WebSessionIdClaim);

        // Response của phiên cũ không được làm mất danh tính phiên khác.
        if (!Guid.TryParse(currentSessionIdValue, out Guid currentSessionId) ||
            currentSessionId != webSessionId)
        {
            return;
        }

        ClaimsPrincipal anonymousUser =
            new ClaimsPrincipal(new ClaimsIdentity());

        SetAuthenticationState(Task.FromResult(
            new AuthenticationState(anonymousUser)));
    }
}