using System.Net.Http.Json;
using TuanKietBranchFlow.Application.DTOs.OrderMenus;

namespace TuanKietBranchFlow.Web.Services;

public class OrderMenuApiService
{
    private readonly AuthorizedApiService _authorizedApiService;

    public OrderMenuApiService(AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService = authorizedApiService;
    }

    // Lấy menu đang bán tại chi nhánh hiện tại của employee
    public async Task<OrderMenuResponseDTO?> GetOrderMenuAsync(int branchId)
    {
        if (branchId <= 0)
        {
            return null;
        }

        string requestUrl = $"api/order-menu?branchId={branchId}";

        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Get, requestUrl);
            
        // Đọc token và gửi request đến API
        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        // API có thể trả 400, 401, 403, 404 hoặc 500
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        // Chuyển JSON menu thành DTO dùng cho giao diện
        OrderMenuResponseDTO? menu =
            await response.Content.ReadFromJsonAsync<OrderMenuResponseDTO>();
        
        return menu;

    }
}