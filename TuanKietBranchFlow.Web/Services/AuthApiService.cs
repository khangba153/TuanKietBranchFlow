using System.Net.Http.Json;
using TuanKietBranchFlow.Application.DTOs.Auth;
using System.Net;
using System.Net.Http.Headers;

namespace TuanKietBranchFlow.Web.Services;

public class AuthApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    // Nhận IHttpClientFactory từ DI để tạo HttpClient đã cấu hình
    public AuthApiService(
        IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    // Gửi thông tin đăng nhập và nhận cặp token do API cấp
    public async Task<LoginResponseDTO?> LoginAsync(
        LoginRequestDTO request)
    {
        HttpClient httpClient =
            _httpClientFactory.CreateClient("BranchFlowApi");

        using HttpResponseMessage response =
            await httpClient.PostAsJsonAsync(
                "api/auth/login",
                request);

        // API xác nhận thông tin đăng nhập không hợp lệ
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        // Không biến lỗi API thành thông báo sai tài khoản hoặc mật khẩu
        response.EnsureSuccessStatusCode();

        LoginResponseDTO? loginResponse =
            await response.Content.ReadFromJsonAsync<LoginResponseDTO>();

        if (loginResponse == null)
        {
            throw new InvalidOperationException(
                "API trả đăng nhập thành công nhưng không có dữ liệu token.");
        }

        return loginResponse;
    }

    // Lấy hồ sơ bằng access token vừa được API cấp khi đăng nhập
    public async Task<CurrentUserDTO?> GetCurrentUserByAccessTokenAsync(
        string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        HttpClient httpClient =
            _httpClientFactory.CreateClient("BranchFlowApi");

        using HttpRequestMessage request = new HttpRequestMessage(
            HttpMethod.Get,
            "api/auth/me");

        // Gắn token riêng cho request, không sửa header chung của client
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        using HttpResponseMessage response =
            await httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        // Lỗi hệ thống không được hiểu nhầm là token không hợp lệ
        response.EnsureSuccessStatusCode();

        CurrentUserDTO? currentUser =
            await response.Content.ReadFromJsonAsync<CurrentUserDTO>();

        if (currentUser == null)
        {
            throw new InvalidOperationException(
                "API trả thành công nhưng không có hồ sơ người dùng.");
        }

        return currentUser;
    }

    // Gọi API thu hồi các phiên bằng access token do Web server quản lý.
    public async Task<bool> LogoutByAccessTokenAsync(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return false;
        }

        HttpClient httpClient =
            _httpClientFactory.CreateClient("BranchFlowApi");

        using HttpRequestMessage request = new HttpRequestMessage(
            HttpMethod.Post,
            "api/auth/logout");

        // API xác định tài khoản từ JWT, không gửi UserId trong body.
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        using HttpResponseMessage response =
            await httpClient.SendAsync(request);

        // API chỉ xác nhận thành công sau khi lưu việc thu hồi phiên.
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return true;
        }

        // Token không được chấp nhận; không khẳng định đã thu hồi mọi phiên.
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return false;
        }

        // Lỗi kết nối hoặc API không được xem là đăng xuất thành công.
        response.EnsureSuccessStatusCode();

        throw new InvalidOperationException(
            "API đăng xuất trả mã thành công không đúng hợp đồng.");
    }
}
