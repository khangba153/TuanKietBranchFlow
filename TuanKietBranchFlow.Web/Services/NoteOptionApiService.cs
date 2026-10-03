using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TuanKietBranchFlow.Application.DTOs.MenuManagement;
using TuanKietBranchFlow.Web.Models;

namespace TuanKietBranchFlow.Web.Services;

public class NoteOptionApiService
{
    private readonly AuthorizedApiService _authorizedApiService;

    public NoteOptionApiService(AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService = authorizedApiService;
    }

    // Gửi yêu cầu tạo lựa chọn ghi chú và chuyển response thành kết quả cho Web
    public async Task<NoteOptionCreateApiResult> CreateNoteOptionAsync(
        CreateNoteOptionRequestDTO requestDTO)
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Post, "api/note-options")
            {
                Content = JsonContent.Create(requestDTO)
            };

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        int statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            MenuNoteOptionDTO? noteOption =
                await response.Content.ReadFromJsonAsync<MenuNoteOptionDTO>();

            if (noteOption == null)
            {
                return new NoteOptionCreateApiResult
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessage = "API không trả về thông tin lựa chọn ghi chú vừa tạo."
                };
            }

            return new NoteOptionCreateApiResult
            {
                IsSuccess = true,
                StatusCode = statusCode,
                NoteOption = noteOption
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

        return new NoteOptionCreateApiResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }

    // Gửi yêu cầu cập nhật lựa chọn ghi chú và chuyển response thành kết quả cho Web
    public async Task<NoteOptionUpdateApiResult> UpdateNoteOptionAsync(
        int noteOptionId,
        UpdateNoteOptionRequestDTO requestDTO)
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Put, $"api/note-options/{noteOptionId}")
            {
                Content = JsonContent.Create(requestDTO)
            };

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        int statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            MenuNoteOptionDTO? noteOption =
                await response.Content.ReadFromJsonAsync<MenuNoteOptionDTO>();

            if (noteOption == null)
            {
                return new NoteOptionUpdateApiResult
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessage = "API không trả về thông tin lựa chọn ghi chú đã cập nhật."
                };
            }

            return new NoteOptionUpdateApiResult
            {
                IsSuccess = true,
                StatusCode = statusCode,
                NoteOption = noteOption
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

        return new NoteOptionUpdateApiResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }

    // Gửi yêu cầu xóa mềm lựa chọn ghi chú và trả kết quả cho Web
    public async Task<NoteOptionDeleteApiResult> DeleteNoteOptionAsync(
        int noteOptionId)
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"api/note-options/{noteOptionId}");

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        int statusCode = (int)response.StatusCode;

        // API trả 204 khi xóa thành công, không có response body
        if (response.IsSuccessStatusCode)
        {
            return new NoteOptionDeleteApiResult
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

        return new NoteOptionDeleteApiResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }
}
