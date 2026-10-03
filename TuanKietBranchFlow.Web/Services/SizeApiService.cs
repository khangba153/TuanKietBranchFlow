using TuanKietBranchFlow.Application.DTOs.MenuManagement;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TuanKietBranchFlow.Web.Models;

namespace TuanKietBranchFlow.Web.Services;

public class SizeApiService
{
    private readonly AuthorizedApiService _authorizedApiService;

    public SizeApiService(AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService = authorizedApiService;
    }

    // Lấy danh sách size từ API bằng token của người dùng hiện tại
    public async Task<List<MenuSizeDTO>?> GetMenuSizesAsync()
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Get, "api/sizes");

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        // Null biểu thị request thất bại; danh sách rỗng vẫn là thành công
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        List<MenuSizeDTO>? sizes =
            await response.Content.ReadFromJsonAsync<List<MenuSizeDTO>>();

        return sizes;
    }

    // Gửi yêu cầu tạo Size và chuyển response API thành kết quả cho Web
    public async Task<SizeCreateApiResult> CreateSizeAsync(
        CreateSizeRequestDTO requestDTO)
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Post, "api/sizes")
            {
                Content = JsonContent.Create(requestDTO)
            };

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        int statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            MenuSizeDTO? size =
                await response.Content.ReadFromJsonAsync<MenuSizeDTO>();

            if (size == null)
            {
                return new SizeCreateApiResult
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessage = "API không trả về thông tin size vừa tạo."
                };
            }

            return new SizeCreateApiResult
            {
                IsSuccess = true,
                StatusCode = statusCode,
                Size = size
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
            // Một số response lỗi không chứa ProblemDetails
        }

        string errorMessage =
            problemDetails?.Detail
            ?? problemDetails?.Title
            ?? $"API trả về lỗi {statusCode}";

        return new SizeCreateApiResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }

    // Gửi yêu cầu cập nhật Size và chuyển response API thành kết quả cho Web
    public async Task<SizeUpdateApiResult> UpdateSizeAsync(
        int sizeId,
        UpdateSizeRequestDTO requestDTO)
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Put, $"api/sizes/{sizeId}")
            {
                Content = JsonContent.Create(requestDTO)
            };

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        int statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            MenuSizeDTO? size =
                await response.Content.ReadFromJsonAsync<MenuSizeDTO>();

            if (size == null)
            {
                return new SizeUpdateApiResult
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessage = "API không trả về thông tin size đã cập nhật."
                };
            }

            return new SizeUpdateApiResult
            {
                IsSuccess = true,
                StatusCode = statusCode,
                Size = size
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
            // Một số response lỗi không chứa ProblemDetails
        }

        string errorMessage =
            problemDetails?.Detail
            ?? problemDetails?.Title
            ?? $"API trả về lỗi {statusCode}";

        return new SizeUpdateApiResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }

    // Gửi yêu cầu xóa mềm Size và chuyển response API thành kết quả cho Web
    public async Task<SizeDeleteApiResult> DeleteSizeAsync(int sizeId)
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Delete, $"api/sizes/{sizeId}");

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        int statusCode = (int)response.StatusCode;

        // API trả 204 khi xóa thành công, không có response body
        if (response.IsSuccessStatusCode)
        {
            return new SizeDeleteApiResult
            {
                IsSuccess = true,
                StatusCode = statusCode
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
            // Một số response lỗi không chứa ProblemDetails
        }

        string errorMessage =
            problemDetails?.Detail
            ?? problemDetails?.Title
            ?? $"API trả về lỗi {statusCode}";

        return new SizeDeleteApiResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }
}
