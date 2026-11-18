using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.Branches;
using TuanKietBranchFlow.Application.DTOs.Orders.Management;
using TuanKietBranchFlow.Web.Services.Common;

namespace TuanKietBranchFlow.Web.Services.Orders;

public class OrderManagementApiService
{
    private readonly AuthorizedApiService _api;
    public OrderManagementApiService(AuthorizedApiService api)
    {
        _api = api;
    }

    // Giữ status code để màn hình phân biệt hết phiên với lỗi kết nối.
    public async Task<List<AccessibleBranchDTO>> GetBranchesAsync()
    {
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/branches/accessible");
        using HttpResponseMessage response = await _api.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<AccessibleBranchDTO>>() ?? throw new InvalidOperationException("API không trả phạm vi chi nhánh.");
    }

    // Mã hóa bộ lọc và dùng token của đúng phiên Web, không dùng client công khai.
    public async Task<OrderManagementPageDTO> GetOrdersAsync(OrderManagementQueryDTO query)
    {
        string url = $"api/order-management?page={query.Page}&pageSize={query.PageSize}";
        if (query.BranchId.HasValue)
        {
            url += $"&branchId={query.BranchId.Value}";
        }

        if (query.BusinessDate.HasValue)
        {
            url += $"&businessDate={query.BusinessDate.Value:yyyy-MM-dd}";
        }

        url += "&keyword=" + Uri.EscapeDataString(query.Keyword?.Trim() ?? string.Empty);
        url += "&status=" + Uri.EscapeDataString(query.Status ?? string.Empty);
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
        using HttpResponseMessage response = await _api.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderManagementPageDTO>() ?? throw new InvalidOperationException("API không trả dữ liệu danh sách đơn.");
    }

    // Đọc chi tiết và lịch sử xử lý trong phạm vi quản lý.
    public async Task<OrderManagementDetailDTO> GetDetailAsync(int orderId)
    {
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"api/order-management/{orderId}");
        using HttpResponseMessage response = await _api.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderManagementDetailDTO>() ?? throw new InvalidOperationException("API không trả dữ liệu chi tiết đơn.");
    }

    // Không tự retry thao tác ghi: mất response cần tải lại trạng thái đơn trước.
    public async Task ResolveReviewAsync(
        int orderId,
        ResolveOrderReviewRequestDTO data)
    {
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"api/order-management/{orderId}/review")

        {

            Content = JsonContent.Create(data)

        };
        using HttpResponseMessage response = await _api.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    // Options và preview dùng cùng Bearer đúng phiên; không tự retry POST ghi.
    public async Task<OrderAdjustmentResultDTO> GetAdjustmentOptionsAsync(int orderId)
    {
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"api/order-management/{orderId}/adjustment/options");
        using HttpResponseMessage response = await _api.SendAsync(request);
        await EnsureAdjustmentSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<OrderAdjustmentResultDTO>() ?? throw new InvalidOperationException("Thiếu dữ liệu điều chỉnh.");
    }

    public async Task<OrderAdjustmentResultDTO> AdjustAsync(
        int orderId,
        AdjustOrderRequestDTO data,
        bool preview)
    {
        using HttpRequestMessage request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/order-management/{orderId}/adjustment" + (preview ? "/preview" : ""))

        {

            Content = JsonContent.Create(data)

        };
        using HttpResponseMessage response = await _api.SendAsync(request);
        await EnsureAdjustmentSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<OrderAdjustmentResultDTO>() ?? throw new InvalidOperationException("Thiếu dữ liệu điều chỉnh.");
    }

    // Chỉ lấy thông báo ProblemDetails đã được API kiểm soát, không hiển thị body/stack trace tùy ý.
    private static async Task EnsureAdjustmentSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string message = "Không thể xử lý điều chỉnh.";
        try
        {
            ProblemDetails? problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
            message = problem?.Detail ?? problem?.Title ?? message;
        }
        catch (System.Text.Json.JsonException)
        {
        }

        throw new HttpRequestException(message, null, response.StatusCode);
    }
}
