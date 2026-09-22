using System.Net.Http.Json;
using TuanKietBranchFlow.Application.DTOs.Users;

namespace TuanKietBranchFlow.Web.Services;

public class UserApiService
{
    private readonly AuthorizedApiService _authorizedApiService;

    public UserApiService(AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService = authorizedApiService;
    }

    // Lấy hồ sơ người dùng đang đăng nhập
    public async Task<UserProfileDTO?> GetCurrentProfileAsync()
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Get, "api/users/me");
        
        // AuthorizedApiService đọc token và gửi request đến API
        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);
        
        // API có thể trả 401, 404 hoặc lỗi hệ thống
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        // Chuyển JSON thành hồ sơ có CurrentBranchId
        UserProfileDTO? profile =
            await response.Content.ReadFromJsonAsync<UserProfileDTO>();
        
        return profile;
        

    }
}