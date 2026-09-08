using System.Net.Http.Json;
using TuanKietBranchFlow.Application.DTOs.Auth;
using System.Net.Http.Headers;

namespace TuanKietBranchFlow.Web.Services;

public class AuthApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    // Nhận IHttpClientFactory từ DI để tạo HttpClient đã cấu hình
    public AuthApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
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
    public async Task<CurrentUserDTO?> GetCurrentUserAsync(string accessToken)
    {
        // Không gọi API nếu chưa có token
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }
        
        // Lấy HttpClient đã cấu hình địa chỉ API
        HttpClient httpClient = _httpClientFactory.CreateClient("BranchFlowApi");

        // Tạo request GET đến endpoint
        HttpRequestMessage  request =
            new HttpRequestMessage(HttpMethod.Get, "api/auth/me");

        // Gắn JWT vào Authorization header
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        // Gửi request đến API
        HttpResponseMessage response =
            await httpClient.SendAsync(request);
        
        // Token không hợp lệ hoặc hết hạn sẽ không trả DTO
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        // Chuyển JSON thành CurrentUserDTO
        CurrentUserDTO? currentUser =
            await response.Content.ReadFromJsonAsync<CurrentUserDTO>();

        return currentUser;        
    }
}