using TuanKietBranchFlow.Application.DTOs.MenuManagement;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TuanKietBranchFlow.Web.Models;

namespace TuanKietBranchFlow.Web.Services;

public class ProductApiService
{
    private readonly AuthorizedApiService _authorizedApiService;

    public ProductApiService(AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService = authorizedApiService;
    }

    // Lấy danh sách Product từ API bằng token của người dùng hiện tại
    public async Task<List<MenuProductDTO>?> GetMenuProductsAsync()
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Get, "api/products");

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        // Null báo request thất bại; danh sách rỗng vẫn là kết quả thành công
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        List<MenuProductDTO>? products =
            await response.Content.ReadFromJsonAsync<List<MenuProductDTO>>();

        return products;
    }

    // Gửi yêu cầu tạo Product cùng các size–giá
    public async Task<ProductCreateApiResult> CreateProductAsync(
        CreateProductRequestDTO requestDTO)
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Post, "api/products")
            {
                Content = JsonContent.Create(requestDTO)
            };

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        int statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            MenuProductDTO? product =
                await response.Content.ReadFromJsonAsync<MenuProductDTO>();

            if (product == null)
            {
                return new ProductCreateApiResult
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessage = "API không trả về thông tin sản phẩm vừa tạo."
                };
            }

            return new ProductCreateApiResult
            {
                IsSuccess = true,
                StatusCode = statusCode,
                Product = product
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

        return new ProductCreateApiResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }

    // Gửi yêu cầu cập nhật Product cùng các size–giá
    public async Task<ProductUpdateApiResult> UpdateProductAsync(
        int productId,
        UpdateProductRequestDTO requestDTO)
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Put, $"api/products/{productId}")
            {
                Content = JsonContent.Create(requestDTO)
            };

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        int statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            MenuProductDTO? product =
                await response.Content.ReadFromJsonAsync<MenuProductDTO>();

            if (product == null)
            {
                return new ProductUpdateApiResult
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessage = "API không trả về thông tin sản phẩm đã cập nhật."
                };
            }

            return new ProductUpdateApiResult
            {
                IsSuccess = true,
                StatusCode = statusCode,
                Product = product
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

        return new ProductUpdateApiResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }

    // Gửi yêu cầu xóa mềm Product và đọc kết quả từ API
    public async Task<ProductDeleteApiResult> DeleteProductAsync(int productId)
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Delete, $"api/products/{productId}");

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        int statusCode = (int)response.StatusCode;

        // API trả 204 khi xóa thành công, không có response body
        if (response.IsSuccessStatusCode)
        {
            return new ProductDeleteApiResult
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

        return new ProductDeleteApiResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }
}
