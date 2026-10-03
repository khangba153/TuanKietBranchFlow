using TuanKietBranchFlow.Application.DTOs.MenuManagement;
using TuanKietBranchFlow.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace TuanKietBranchFlow.Web.Services;

public class CategoryApiService
{
    private readonly AuthorizedApiService _authorizedApiService;

    public CategoryApiService(AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService = authorizedApiService;
    }

    // Lấy danh mục từ API bằng token của người dùng hiện tại
    public async Task<List<MenuCategoryDTO>?> GetMenuCategoriesAsync()
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Get, "api/categories");

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        // Null biểu thị request thất bại; danh sách rỗng là kết quả thành công
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        List<MenuCategoryDTO>? categories  =
            await response.Content.ReadFromJsonAsync<List<MenuCategoryDTO>>();

        return categories;
    }

    // Gửi yêu cầu tạo danh mục và đọc kết quả từ API
    public async Task<CategoryCreateApiResult> CreateCategoryAsync(
        CreateCategoryRequestDTO requestDTO)
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Post, "api/categories")
            {
                Content = JsonContent.Create(requestDTO)
            };

        // Đọc và gắn Bearer vào request
        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        int statusCode = (int)response.StatusCode;

        // API trả thành công thì đọc danh mục vừa tạo
        if (response.IsSuccessStatusCode)
        {
            MenuCategoryDTO? category =
                await response.Content.ReadFromJsonAsync<MenuCategoryDTO>();

            if (category == null)
            {
                return new CategoryCreateApiResult
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessage = "API không trả về thông tin danh mục vừa tạo."
                };
            }

            return new CategoryCreateApiResult
            {
                IsSuccess = true,
                StatusCode = statusCode,
                Category = category
            };
        }

        ProblemDetails? problemDetails = null;

        try
        {
            // Đọc lỗi 400, 401, 403, 409 hoặc 500 từ API
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

        return new CategoryCreateApiResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }

    // Gửi yêu cầu cập nhật danh mục và đọc kết quả từ API
    public async Task<CategoryUpdateApiResult> UpdateCategoryAsync(
        int categoryId,
        UpdateCategoryRequestDTO requestDTO)
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Put, $"api/categories/{categoryId}")
            {
                Content = JsonContent.Create(requestDTO)
            };

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        int statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            MenuCategoryDTO? category =
                await response.Content.ReadFromJsonAsync<MenuCategoryDTO>();

            if (category == null)
            {
                return new CategoryUpdateApiResult
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessage = "API không trả về thông tin danh mục đã cập nhật."
                };
            }

            return new CategoryUpdateApiResult
            {
                IsSuccess = true,
                StatusCode = statusCode,
                Category = category
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

        return new CategoryUpdateApiResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }

    // Gửi yêu cầu xóa mềm danh mục và đọc kết quả từ API
    public async Task<CategoryDeleteApiResult> DeleteCategoryAsync(int categoryId)
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Delete, $"api/categories/{categoryId}");

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        int statusCode = (int)response.StatusCode;

        // API trả 204 khi xóa thành công, không có response body để đọc
        if (response.IsSuccessStatusCode)
        {
            return new CategoryDeleteApiResult
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
            // Một số reponse lỗi không chứa ProblemDetails
        }

        string errorMessage =
            problemDetails?.Detail
            ?? problemDetails?.Title
            ?? $"API trả về lỗi {statusCode}";

        return new CategoryDeleteApiResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }
}