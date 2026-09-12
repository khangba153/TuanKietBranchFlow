
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Web.Models;
using System.Net.Http.Json;
using TuanKietBranchFlow.Application.DTOs.Employees;

namespace TuanKietBranchFlow.Web.Services;

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