using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net;

namespace TuanKietBranchFlow.Web.Services;

public class AuthorizedApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IWebTokenService _webTokenService;

    public AuthorizedApiService(
        IHttpClientFactory httpClientFactory,
        AuthenticationStateProvider authenticationStateProvider,
        IWebTokenService webTokenService)
    {
        _httpClientFactory = httpClientFactory;
        _authenticationStateProvider = authenticationStateProvider;
        _webTokenService = webTokenService;
    }

    // Lấy token của đúng phiên Web và xử lý khi API từ chối xác thực.
    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request)
    {
        // Không sử dụng Authorization có sẵn do nơi gọi truyền vào.
        request.Headers.Authorization = null;

        Guid? webSessionId = null;

        AuthenticationState authenticationState =
            await _authenticationStateProvider.GetAuthenticationStateAsync();

        ClaimsPrincipal user = authenticationState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            string? sessionIdValue = user.FindFirstValue(
                WebCookieAuthenticationEvents.WebSessionIdClaim);

            if (Guid.TryParse(sessionIdValue, out Guid sessionId) &&
                sessionId != Guid.Empty)
            {
                // Giữ ID của phiên đã dùng để gửi request này.
                webSessionId = sessionId;

                string? accessToken =
                    await _webTokenService.GetAccessTokenAsync(sessionId);

                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue(
                            "Bearer",
                            accessToken);
                }
            }
        }

        HttpClient httpClient =
            _httpClientFactory.CreateClient("BranchFlowApi");

        HttpResponseMessage response =
            await httpClient.SendAsync(request);

        // 401 là xác thực không được chấp nhận; 403 không làm mất phiên.
        if (response.StatusCode == HttpStatusCode.Unauthorized &&
            webSessionId.HasValue &&
            _authenticationStateProvider is WebAuthenticationStateProvider provider)
        {
            await provider.RejectSessionAsync(webSessionId.Value);
        }

        // Nơi gọi tiếp tục đọc và giải phóng response.
        return response;
    }
}