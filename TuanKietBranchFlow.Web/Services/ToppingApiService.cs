using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TuanKietBranchFlow.Application.DTOs.MenuManagement;
using TuanKietBranchFlow.Web.Models;

namespace TuanKietBranchFlow.Web.Services;

public class ToppingApiService
{
    private readonly AuthorizedApiService _authorizedApiService;

    public ToppingApiService(AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService = authorizedApiService;
    }

    // Gửi yêu cầu tạo topping và chuyển response thành kết quả cho Web
    public async Task<ToppingCreateApiResult> CreateToppingAsync(
        CreateToppingRequestDTO requestDTO)
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Post, "api/toppings")
            {
                Content = JsonContent.Create(requestDTO)
            };

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        int statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            MenuToppingDTO? topping =
                await response.Content.ReadFromJsonAsync<MenuToppingDTO>();

            if (topping == null)
            {
                return new ToppingCreateApiResult
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessage = "API không trả về thông tin topping vừa tạo."
                };
            }

            return new ToppingCreateApiResult
            {
                IsSuccess = true,
                StatusCode = statusCode,
                Topping = topping
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

        return new ToppingCreateApiResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }

    // Gửi yêu cầu cập nhật topping và chuyển response thành kết quả cho Web
    public async Task<ToppingUpdateApiResult> UpdateToppingAsync(
        int toppingId,
        UpdateToppingRequestDTO requestDTO)
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Put, $"api/toppings/{toppingId}")
            {
                Content = JsonContent.Create(requestDTO)
            };

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        int statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            MenuToppingDTO? topping =
                await response.Content.ReadFromJsonAsync<MenuToppingDTO>();

            if (topping == null)
            {
                return new ToppingUpdateApiResult
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessage = "API không trả về thông tin topping đã cập nhật."
                };
            }

            return new ToppingUpdateApiResult
            {
                IsSuccess = true,
                StatusCode = statusCode,
                Topping = topping
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

        return new ToppingUpdateApiResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }

    // Gửi yêu cầu xóa mềm topping và trả kết quả cho Web
    public async Task<ToppingDeleteApiResult> DeleteToppingAsync(
        int toppingId)
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"api/toppings/{toppingId}");

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        int statusCode = (int)response.StatusCode;

        // API trả 204 khi xóa thành công, không có response body
        if (response.IsSuccessStatusCode)
        {
            return new ToppingDeleteApiResult
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

        return new ToppingDeleteApiResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }
}