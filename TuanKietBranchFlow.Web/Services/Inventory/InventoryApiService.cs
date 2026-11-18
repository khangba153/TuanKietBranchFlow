using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.Inventory;
using TuanKietBranchFlow.Web.Services.Common;

namespace TuanKietBranchFlow.Web.Services.Inventory;

public class InventoryApiService
{
    private readonly AuthorizedApiService _authorizedApiService;
    public InventoryApiService(AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService = authorizedApiService;
    }

    public Task<List<InventoryUnitDTO>> GetUnitsAsync()
    {
        return GetAsync<List<InventoryUnitDTO>>("api/ingredient-units");
    }

    public Task<List<InventoryIngredientDTO>> GetIngredientsAsync()
    {
        return GetAsync<List<InventoryIngredientDTO>>("api/ingredients");
    }

    public Task<InventoryPageDTO> GetStockAsync(InventoryQueryDTO query)
    {
        string url = $"api/inventory?branchId={query.BranchId}&page={query.Page}" + $"&search={Uri.EscapeDataString(query.Search ?? "")}&stockStatus={Uri.EscapeDataString(query.StockStatus ?? "")}";
        if (query.IsActive.HasValue)
        {
            url += $"&isActive={query.IsActive.Value.ToString().ToLowerInvariant()}";
        }

        return GetAsync<InventoryPageDTO>(url);
    }

    private async Task<T> GetAsync<T>(string url)
    {
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
        using HttpResponseMessage response = await _authorizedApiService.SendAsync(request);
        await CheckResponseAsync(response);
        return await response.Content.ReadFromJsonAsync<T>() ?? throw new JsonException("Không đọc được dữ liệu kho.");
    }

    // Không retry ghi; caller khóa form khi không xác định server đã lưu hay chưa.
    public async Task WriteAsync<T>(
        HttpMethod method,
        string url,
        T input)
    {
        using HttpRequestMessage request = new HttpRequestMessage(method, url)

        {

            Content = JsonContent.Create(input)

        };
        using HttpResponseMessage response = await _authorizedApiService.SendAsync(request);
        await CheckResponseAsync(response);
        InventoryResultDTO? result = await response.Content.ReadFromJsonAsync<InventoryResultDTO>();
        if (result?.SavedId == null)
        {
            throw new JsonException("Cần tải lại để đối chiếu kết quả thực tế.");
        }
    }

    private static async Task CheckResponseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string message = $"Không thể xử lý yêu cầu (HTTP {(int)response.StatusCode}).";
        try
        {
            ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            message = problem?.Detail ?? message;
        }
        catch (JsonException)
        {
            // Không hiển thị HTML hoặc dữ liệu lỗi thô từ proxy.
        }

        throw new HttpRequestException(message, null, response.StatusCode);
    }
}
