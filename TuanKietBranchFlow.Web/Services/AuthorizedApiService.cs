using System.Net.Http.Headers;
using Microsoft.JSInterop;

namespace TuanKietBranchFlow.Web.Services;

public class AuthorizedApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILocalStorageService _localStorageService;

    public AuthorizedApiService(
        IHttpClientFactory httpClientFactory,
        ILocalStorageService localStorageService)
    {
        _httpClientFactory = httpClientFactory;
        _localStorageService = localStorageService;
    }

    // Gán accessToken và gửi request đến API
    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        // Đọc access token trong đúng Blazor circuit
        string? accessToken =
            await _localStorageService.GetItemAsync<string>("accessToken");

        // Có token thì gán vào Authorization header
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
        }

        // Dùng client có BaseAddress của API
        HttpClient httpClient =
            _httpClientFactory.CreateClient("BranchFlowApi");

        return await httpClient.SendAsync(request);
    }
}
