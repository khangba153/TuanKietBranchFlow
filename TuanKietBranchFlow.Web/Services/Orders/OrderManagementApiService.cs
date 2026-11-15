using System.Net.Http.Json;
using TuanKietBranchFlow.Application.DTOs.Branches;
using TuanKietBranchFlow.Application.DTOs.Orders.Management;
using TuanKietBranchFlow.Web.Services.Common;

namespace TuanKietBranchFlow.Web.Services.Orders;

public class OrderManagementApiService
{
    private readonly AuthorizedApiService _api;
    public OrderManagementApiService(AuthorizedApiService api) { _api = api; }

    // Giữ status code để màn hình phân biệt hết phiên với lỗi kết nối.
    public async Task<List<AccessibleBranchDTO>> GetBranchesAsync()
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "api/branches/accessible");
        using HttpResponseMessage response = await _api.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<AccessibleBranchDTO>>()
            ?? throw new InvalidOperationException("API không trả phạm vi chi nhánh.");
    }

    // Mã hóa bộ lọc và dùng token của đúng phiên Web, không dùng client công khai.
    public async Task<OrderManagementPageDTO> GetOrdersAsync(OrderManagementQueryDTO query)
    {
        string url = $"api/order-management?page={query.Page}&pageSize={query.PageSize}";
        if (query.BranchId.HasValue) url += $"&branchId={query.BranchId.Value}";
        if (query.BusinessDate.HasValue) url += $"&businessDate={query.BusinessDate.Value:yyyy-MM-dd}";
        url += "&keyword=" + Uri.EscapeDataString(query.Keyword?.Trim() ?? string.Empty);
        url += "&status=" + Uri.EscapeDataString(query.Status ?? string.Empty);
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        using HttpResponseMessage response = await _api.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderManagementPageDTO>()
            ?? throw new InvalidOperationException("API không trả dữ liệu danh sách đơn.");
    }

    // Luồng này chỉ GET, không cung cấp thao tác sửa hoặc hủy đơn.
    public async Task<OrderManagementDetailDTO> GetDetailAsync(int orderId)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, $"api/order-management/{orderId}");
        using HttpResponseMessage response = await _api.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderManagementDetailDTO>()
            ?? throw new InvalidOperationException("API không trả dữ liệu chi tiết đơn.");
    }
}
