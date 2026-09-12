using System.Net.Http.Json;
using TuanKietBranchFlow.Application.DTOs.Branches;

namespace TuanKietBranchFlow.Web.Services;

public class BranchApiService
{
    private readonly AuthorizedApiService _authorizedApiService;

    public BranchApiService(AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService = authorizedApiService;
    }

    // Gọi API đế lấy các chi nhánh người dùng được phép truy cập
    public async Task<List<AccessibleBranchDTO>?> GetAccessibleBranchesAsync()
    {
        // Tạo request gọi endpoint 
        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Get, "api/branches/accessible");
        
        // AuthorizedApiService đọc token và gắn vào bearer
        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);
        
        // API trả 401, 403 hoặc lỗi khác thì xem là gọi thất bại
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        // Chuyển JSON trong response thành danh sách DTO
        List<AccessibleBranchDTO>? branches =
            await response.Content.ReadFromJsonAsync<List<AccessibleBranchDTO>>();

        return branches;
    }
}