using TuanKietBranchFlow.Application.DTOs.MenuManagement;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TuanKietBranchFlow.Web.Models;

namespace TuanKietBranchFlow.Web.Services;

public class ToppingGroupApiService
{
    private readonly AuthorizedApiService _authorizedApiService;

    public ToppingGroupApiService(
        AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService = authorizedApiService;
    }

    // Lấy nhóm topping từ API bằng token của người dùng hiện tại
    public async Task<List<MenuToppingGroupDTO>?> GetMenuToppingGroupsAsync()
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Get, "api/topping-groups");

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        // Null biểu thị request thất bại; danh sách rỗng vẫn là thành công
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        List<MenuToppingGroupDTO>? toppingGroups =
            await response.Content.ReadFromJsonAsync<List<MenuToppingGroupDTO>>();

        return toppingGroups;
    }

    // Gửi yêu cầu tạo nhóm topping và chuyển response thành kết quả cho Web
    public async Task<ToppingGroupCreateApiResult> CreateToppingGroupAsync(
        CreateToppingGroupRequestDTO requestDTO)
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Post, "api/topping-groups")
            {
                Content = JsonContent.Create(requestDTO)
            };

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        int statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            MenuToppingGroupDTO? toppingGroup =
                await response.Content.ReadFromJsonAsync<MenuToppingGroupDTO>();

            if (toppingGroup == null)
            {
                return new ToppingGroupCreateApiResult
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessage = "API không trả về thông tin nhóm topping vừa tạo."
                };
            }

            return new ToppingGroupCreateApiResult
            {
                IsSuccess = true,
                StatusCode = statusCode,
                ToppingGroup = toppingGroup
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

        return new ToppingGroupCreateApiResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }

    // Gửi yêu cầu cập nhật nhóm topping và chuyển response thành kết quả cho Web
    public async Task<ToppingGroupUpdateApiResult> UpdateToppingGroupAsync(
        int toppingGroupId,
        UpdateToppingGroupRequestDTO requestDTO)
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(
                HttpMethod.Put,
                $"api/topping-groups/{toppingGroupId}")
            {
                Content = JsonContent.Create(requestDTO)
            };

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        int statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            MenuToppingGroupDTO? toppingGroup =
                await response.Content.ReadFromJsonAsync<MenuToppingGroupDTO>();

            if (toppingGroup == null)
            {
                return new ToppingGroupUpdateApiResult
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessage = "API không trả về thông tin nhóm topping đã cập nhật."
                };
            }

            return new ToppingGroupUpdateApiResult
            {
                IsSuccess = true,
                StatusCode = statusCode,
                ToppingGroup = toppingGroup
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

        return new ToppingGroupUpdateApiResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }

    // Gửi yêu cầu xóa mềm nhóm topping và trả kết quả cho Web
    public async Task<ToppingGroupDeleteApiResult> DeleteToppingGroupAsync(
        int toppingGroupId)
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"api/topping-groups/{toppingGroupId}");

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        int statusCode = (int)response.StatusCode;

        // API trả 204 khi xóa thành công, không có response body
        if (response.IsSuccessStatusCode)
        {
            return new ToppingGroupDeleteApiResult
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

        return new ToppingGroupDeleteApiResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }
}