using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.Employees.Transfers;
using TuanKietBranchFlow.Web.Services.Common;

namespace TuanKietBranchFlow.Web.Services.Employees;

public class EmployeeTransferApiService
{
    private readonly AuthorizedApiService _authorizedApiService;
    public EmployeeTransferApiService(AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService = authorizedApiService;
    }

    public async Task<List<EmployeeTransferTargetDTO>> GetTargetsAsync(int branchId)
    {
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"api/employee-transfers/targets?sourceBranchId={branchId}");
        using HttpResponseMessage response = await _authorizedApiService.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<EmployeeTransferTargetDTO>>() ?? throw new JsonException("Không đọc được danh mục chi nhánh.");
    }

    public async Task<List<EmployeeTransferDTO>> GetRequestsAsync(
        bool incoming,
        int page)
    {
        using HttpRequestMessage request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/employee-transfers?incoming={incoming.ToString().ToLowerInvariant()}&page={page}");
        using HttpResponseMessage response = await _authorizedApiService.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<EmployeeTransferDTO>>() ?? throw new JsonException("Không đọc được danh sách yêu cầu.");
    }

    public Task<EmployeeTransferDTO> CreateAsync(EmployeeTransferCreateDTO input)
    {
        return SendWriteAsync("api/employee-transfers", input);
    }

    public Task<EmployeeTransferDTO> DecideAsync(
        int id,
        string action,
        string? reason)
    {
        return SendWriteAsync(
            $"api/employee-transfers/{id}/{action}",
            new EmployeeTransferDecisionDTO
            {
                Reason = reason
            });
    }

    // Một POST duy nhất; không tự retry nếu mất phản hồi sau khi server đã lưu.
    private async Task<EmployeeTransferDTO> SendWriteAsync<T>(
        string url,
        T input)
    {
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url)

        {

            Content = JsonContent.Create(input)

        };
        using HttpResponseMessage response = await _authorizedApiService.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            string message = $"Không thể xử lý yêu cầu (HTTP {(int)response.StatusCode}).";
            try
            {
                ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
                message = problem?.Detail ?? message;
            }
            catch (JsonException)
            {
                // Không hiển thị HTML hoặc nội dung proxy.
            }

            throw new HttpRequestException(message, null, response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<EmployeeTransferDTO>() ?? throw new JsonException("Cần tải lại để xác định kết quả thực tế.");
    }
}
