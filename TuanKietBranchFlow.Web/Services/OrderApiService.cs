using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.Orders;
using TuanKietBranchFlow.Web.Models;

namespace TuanKietBranchFlow.Web.Services;

public class OrderApiService
{
    private readonly AuthorizedApiService _authorizedApiService;

    public OrderApiService(AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService = authorizedApiService;
    }

    // Gửi giỏ hàng đến API và đọc kết quả tạo đơn
    public async Task<OrderCreateApiResult> CreateOrderAsync(
        CreateOrderRequestDTO requestDTO)
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Post, "api/orders")
            {
                Content = JsonContent.Create(requestDTO)
            };

        // AuthorizedApiService đọc token và gắn Bearer vào request
        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        int statusCode = (int)response.StatusCode;

        // API trả 201 thì đọc thông tin đơn vừa tạo
        if (response.IsSuccessStatusCode)
        {
            CreateOrderResponseDTO? createOrder =
                await response.Content.ReadFromJsonAsync<CreateOrderResponseDTO>();

            if (createOrder is null)
            {
                return new OrderCreateApiResult
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessage = "API không trả về thông tin đơn vừa tạo."
                };
            }

            return new OrderCreateApiResult
            {
                IsSuccess = true,
                StatusCode = statusCode,
                Order = createOrder
            };
        }

        ProblemDetails? problemDetails = null;

        try
        {
            // Đọc lỗi 400, 401, 403, 404 hoặc 500 từ API
            problemDetails =
                await response.Content.ReadFromJsonAsync<ProblemDetails>();
        }
        catch (JsonException)
        {
            // Một số response lỗi có thể không chứa ProblemDetails
        }

        string errorMessage =
            problemDetails?.Detail
            ?? problemDetails?.Title
            ?? $"API trả về lỗi {statusCode}.";

        return new OrderCreateApiResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }

    // Lấy danh sách đơn do employee hiện tại tạo
    public async Task<List<MyOrderListItemDTO>?> GetMyOrdersAsync(
        string keyword,
        DateOnly? businessDate)
    {
        // Xóa khoảng trắng và mã hóa từ khóa trước khi đưa vào URL
        string encodedKeyword =
            Uri.EscapeDataString(keyword.Trim());

        string requestUrl = $"api/orders/mine?keyword={encodedKeyword}";

        // Chỉ gửi ngày khi người dùng lọc theo ngày
        if (businessDate.HasValue)
        {
            string dateValue = businessDate.Value.ToString("yyyy-MM-dd");

            requestUrl += $"&businessDate={dateValue}";
        }

        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Get, requestUrl);

        // AuthorizedApiService đọc token và gắn Bearer
        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        // API có thể trả lỗi xác thức, phân quyền hoặc dữ liệu query sai
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        List<MyOrderListItemDTO>? orders =
            await response.Content.ReadFromJsonAsync<List<MyOrderListItemDTO>>();

        return orders;
    }

    // Lấy chi tiết 1 đơn do employee hiện tại tạo
    public async Task<OrderDetailApiResult> GetMyOrderDetailAsync(
        int orderId)
    {
        string requestUrl = $"api/orders/{orderId}";

        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Get, requestUrl);

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        int statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            MyOrderDetailDTO? order =
                await response.Content.ReadFromJsonAsync<MyOrderDetailDTO>();

            if (order is null)
            {
                return new OrderDetailApiResult
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessage = "API không trả về thông tin chi tiết đơn."
                };
            }

            return new OrderDetailApiResult
            {
                IsSuccess = true,
                StatusCode = statusCode,
                Order = order
            };
        }

        ProblemDetails? problemDetails = null;

        try
        {
            problemDetails =
                await response.Content.ReadFromJsonAsync<ProblemDetails>();
        }
        catch (JsonException)
        {
            // Một số response lỗi có thể không chứa ProblemDetails
        }

        string errorMessage =
            problemDetails?.Detail
            ?? problemDetails?.Title
            ?? $"API trả về lỗi {statusCode}";

        return new OrderDetailApiResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }

    // Gủi lý do báo sai và đọc trạng thái đơn cập nhật
    public async Task<OrderReportApiResult> ReportOrderAsync(
        int orderId,
        ReportOrderRequestDTO requestDTO)
    {
        string requestUrl = $"api/orders/{orderId}/report";

        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Patch, requestUrl)
            {
                Content = JsonContent.Create(requestDTO)
            };

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        int statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            ReportOrderResponseDTO? order =
                await response.Content
                    .ReadFromJsonAsync<ReportOrderResponseDTO>();

            if (order is null)
            {
                return new OrderReportApiResult
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessage = "API không trả về thông tin đơn vừa báo sai."
                };
            }

            return new OrderReportApiResult
            {
                IsSuccess = true,
                StatusCode = statusCode,
                Order = order
            };
        }

        ProblemDetails? problemDetails = null;

        try
        {
            problemDetails =
                await response.Content.ReadFromJsonAsync<ProblemDetails>();
        }
        catch (JsonException)
        {
            // Một số response lỗi có thể không chứa ProblemDetails
        }

        string errorMessage =
            problemDetails?.Detail
            ?? problemDetails?.Title
            ?? $"API trả về lỗi {statusCode}.";

        return new OrderReportApiResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }
}