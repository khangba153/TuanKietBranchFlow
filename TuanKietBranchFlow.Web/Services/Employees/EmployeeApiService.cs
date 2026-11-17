using TuanKietBranchFlow.Web.Models.Employees;
using TuanKietBranchFlow.Web.Services.Common;

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using TuanKietBranchFlow.Application.DTOs.Employees;

namespace TuanKietBranchFlow.Web.Services.Employees;

public class EmployeeApiService
{
    private readonly AuthorizedApiService _authorizedApiService;

    public EmployeeApiService(AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService = authorizedApiService;
    }

    // Gọi API lấy nhân viên theo chi nhánh và điều kiện lọc
    public async Task<List<EmployeeListItemDTO>?> GetEmployeesAsync(
        int branchId,
        string keyword,
        bool? isActive)
    {
        // Xóa khoản trắng và mã hóa keyword trước khi đưa lên URL
        string encodedKeyword = Uri.EscapeDataString(keyword.Trim());

        string requestUrl =
            $"api/employees?branchId={branchId}" + $"&keyword={encodedKeyword}";

        // Có true hoặc false thì mới thêm điều kiện trạng thái vào URL
        if (isActive.HasValue)
        {
            string activeValue =
                isActive.Value.ToString().ToLowerInvariant();

            requestUrl += $"&isActive={activeValue}";
        }

        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Get, requestUrl);

        // Gắn token và gửi request đến API
        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        // API trả 400, 401, 403, 404 hoặc 500
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        // Chuyển JSON thành danh sách DTO
        List<EmployeeListItemDTO>? employees=
            await response.Content.ReadFromJsonAsync<List<EmployeeListItemDTO>>();

        return employees;
    }

    // Gọi API lấy hồ sơ chi tiết của một nhân viên
    public async Task<EmployeeDetailDTO?> GetEmployeeDetailAsync(
        int employeeId,
        int branchId)
    {
        string requestUrl = $"api/employees/{employeeId}?branchId={branchId}";

        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Get, requestUrl);

        // AuthorizedApiService sẽ đọc token và gắn Bearer vào request
        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        // API có thể trả 400, 401, 403, 404 hoặc 500
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        // Chuyển JSON trong response thành EmployeeDetailDTO
        EmployeeDetailDTO? employee =
            await response.Content.ReadFromJsonAsync<EmployeeDetailDTO>();

        return employee;
    }

    // Đọc hồ sơ cho form sửa và giữ HTTP status để hiển thị hướng xử lý phù hợp.
    public async Task<EmployeeDetailApiResult> GetEmployeeForEditAsync(
        int employeeId,
        int branchId)
    {
        string requestUrl = $"api/employees/{employeeId}?branchId={branchId}";

        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Get, requestUrl);

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        return await ReadEmployeeResultAsync(response);
    }

    // Chỉ gửi một PUT; không tự retry thao tác ghi khi mất phản hồi.
    public async Task<EmployeeDetailApiResult> UpdateEmployeeAsync(
        int employeeId,
        int branchId,
        EmployeeUpdateDTO employeeUpdateDTO)
    {
        string requestUrl = $"api/employees/{employeeId}?branchId={branchId}";

        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Put, requestUrl)
            {
                Content = JsonContent.Create(employeeUpdateDTO)
            };

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        return await ReadEmployeeResultAsync(response);
    }

    // Thay đổi trạng thái một lần; khi mất phản hồi phải đọc lại trước khi thao tác tiếp.
    public async Task<EmployeeDetailApiResult> UpdateEmployeeStatusAsync(
        int employeeId,
        int branchId,
        EmployeeStatusUpdateDTO employeeStatusUpdateDTO)
    {
        string requestUrl = $"api/employees/{employeeId}/status?branchId={branchId}";

        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Patch, requestUrl)
        {
            Content = JsonContent.Create(employeeStatusUpdateDTO)
        };

        using HttpResponseMessage response = await _authorizedApiService.SendAsync(request);

        return await ReadEmployeeResultAsync(response);
    }

    // Không đưa nội dung HTML hoặc lỗi parser từ máy chủ vào giao diện.
    private static async Task<EmployeeDetailApiResult> ReadEmployeeResultAsync(
        HttpResponseMessage response)
    {
        int statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            EmployeeDetailDTO? employee = null;

            try
            {
                employee = await response.Content.ReadFromJsonAsync<EmployeeDetailDTO>();
            }
            catch (JsonException)
            {
                // Response thành công nhưng không đọc được: cần tải lại để biết kết quả thực tế.
            }

            return new EmployeeDetailApiResult
            {
                IsSuccess = employee is not null,
                StatusCode = statusCode,
                Employee = employee,
                ErrorMessage = employee is null
                    ? "Không đọc được hồ sơ trả về. Hãy tải lại để kiểm tra kết quả."
                    : string.Empty
            };
        }

        ValidationProblemDetails? problemDetails = null;

        try
        {
            problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        }
        catch (JsonException)
        {
            // 401 hoặc lỗi proxy có thể không chứa JSON.
        }

        return new EmployeeDetailApiResult
        {
            StatusCode = statusCode,
            ErrorMessage = problemDetails?.Detail
                ?? problemDetails?.Title
                ?? $"Không thể xử lý hồ sơ. HTTP {statusCode}.",
            ValidationErrors = problemDetails?.Errors
                ?? new Dictionary<string, string[]>()
        };
    }

    // Gọi API để tạo tài khoản, hồ sơ và phân công cho nhân viên mới
    public async Task<EmployeeCreateApiResult> CreateEmployeeAsync(
        EmployeeCreateDTO employeeCreateDTO)
    {
        // Tạo request POST và chuyển DTO thành JSON
        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Post, "api/employees")
            {
                Content = JsonContent.Create(employeeCreateDTO)
            };
        
        // Đọc token, gắn Bearer và gửi request đến API
        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);
        
        int statusCode = (int)response.StatusCode;
        
        // API trả thành công thì đọc thông tin nhân viên vừa tạo
        if (response.IsSuccessStatusCode)
        {
            EmployeeDetailDTO? createdEmployee =
                await response.Content.ReadFromJsonAsync<EmployeeDetailDTO>();

            // Status thành công nhưng body không có dữ liệu hợp lệ
            if (createdEmployee == null)
            {
                return new EmployeeCreateApiResult
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessage = "API không trả về thông tin nhân viên vừa tạo."
                };
            }

            return new EmployeeCreateApiResult
            {
                IsSuccess = true,
                StatusCode = statusCode,
                Employee = createdEmployee
            };
        }

        ProblemDetails? problemDetails = null;

        try
        {
            // Đọc nội dung lỗi do EmployeesController trả về
            problemDetails =
                await response.Content.ReadFromJsonAsync<ProblemDetails>();
        }
        catch (JsonException)
        {
            // Một số lỗi như 401 có thể không chứa JSON ProblemDetails
        }

        string errorMessage =
            problemDetails?.Detail
            ?? problemDetails?.Title
            ?? $"API trả về lỗi {statusCode}";

        return new EmployeeCreateApiResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }


}
