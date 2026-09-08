using System.Net.Http.Json;
using TuanKietBranchFlow.Application.DTOs.Auth;

namespace TuanKietBranchFlow.Web.Services;

public class AuthApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AuthorizedApiService _authorizedApiService;
    
    // Nhận IHttpClientFactory từ DI để tạo HttpClient đã cấu hình
    public AuthApiService(
        IHttpClientFactory httpClientFactory,
        AuthorizedApiService authorizedApiService)
    {
        _httpClientFactory = httpClientFactory;
        _authorizedApiService = authorizedApiService;
    }

    // Gửi thông tin đăng nhập đến API và nhận AccessToken
    public async Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO request)
    {
        // Lấy HttpClient có địa chỉ API được đăng ký trong Program
        HttpClient httpClient = _httpClientFactory.CreateClient("BranchFlowApi");

        // Chuyển request thành JSON và gửi đến endpoint đăng nhập
        HttpResponseMessage response =
            await httpClient.PostAsJsonAsync("api/auth/login", request);

        // Trả null nếu đăng nhập thất bại
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        // Chuyển JSON trả về thành LoginResponseDTO
        LoginResponseDTO? loginResponse =
            await response.Content.ReadFromJsonAsync<LoginResponseDTO>();

        return loginResponse;
    }

    // Gọi API để lấy thông tin người dùng JWT
    public async Task<CurrentUserDTO?> GetCurrentUserAsync()
    {
        // Tạo request đến endpoint yêu cầu đăng nhập
        HttpRequestMessage request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "api/auth/me");

        HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        CurrentUserDTO? currentUser =
            await response.Content.ReadFromJsonAsync<CurrentUserDTO>();

        return currentUser;
    }
}
