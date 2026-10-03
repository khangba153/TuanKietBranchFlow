using TuanKietBranchFlow.Application.DTOs.MenuManagement;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TuanKietBranchFlow.Web.Models;

namespace TuanKietBranchFlow.Web.Services;

public class NoteGroupApiService
{
    private readonly AuthorizedApiService _authorizedApiService;

    public NoteGroupApiService(
        AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService = authorizedApiService;
    }

    // Lấy nhóm ghi chú từ API bằng token của người dùng hiện tại
    public async Task<List<MenuNoteGroupDTO>?> GetMenuNoteGroupsAsync()
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Get, "api/note-groups");

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        // Null biểu thị request thất bại; danh sách rỗng vẫn là thành công
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        List<MenuNoteGroupDTO>? noteGroups =
            await response.Content.ReadFromJsonAsync<List<MenuNoteGroupDTO>>();

        return noteGroups;
    }

    // Gửi yêu cầu tạo nhóm ghi chú và chuyển response thành kết quả cho Web
    public async Task<NoteGroupCreateApiResult> CreateNoteGroupAsync(
        CreateNoteGroupRequestDTO requestDTO)
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Post, "api/note-groups")
            {
                Content = JsonContent.Create(requestDTO)
            };

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        int statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            MenuNoteGroupDTO? noteGroup =
                await response.Content.ReadFromJsonAsync<MenuNoteGroupDTO>();

            if (noteGroup == null)
            {
                return new NoteGroupCreateApiResult
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessage = "API không trả về thông tin nhóm ghi chú vừa tạo."
                };
            }

            return new NoteGroupCreateApiResult
            {
                IsSuccess = true,
                StatusCode = statusCode,
                NoteGroup = noteGroup
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

        return new NoteGroupCreateApiResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }

    // Gửi yêu cầu cập nhật nhóm ghi chú và chuyển response thành kết quả cho Web
    public async Task<NoteGroupUpdateApiResult> UpdateNoteGroupAsync(
        int noteGroupId,
        UpdateNoteGroupRequestDTO requestDTO)
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(
                HttpMethod.Put,
                $"api/note-groups/{noteGroupId}")
            {
                Content = JsonContent.Create(requestDTO)
            };

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        int statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            MenuNoteGroupDTO? noteGroup =
                await response.Content.ReadFromJsonAsync<MenuNoteGroupDTO>();

            if (noteGroup == null)
            {
                return new NoteGroupUpdateApiResult
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessage = "API không trả về thông tin nhóm ghi chú đã cập nhật."
                };
            }

            return new NoteGroupUpdateApiResult
            {
                IsSuccess = true,
                StatusCode = statusCode,
                NoteGroup = noteGroup
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

        return new NoteGroupUpdateApiResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }

    // Gửi yêu cầu xóa mềm nhóm ghi chú và trả kết quả cho Web
    public async Task<NoteGroupDeleteApiResult> DeleteNoteGroupAsync(
        int noteGroupId)
    {
        using HttpRequestMessage request =
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"api/note-groups/{noteGroupId}");

        using HttpResponseMessage response =
            await _authorizedApiService.SendAsync(request);

        int statusCode = (int)response.StatusCode;

        // API trả 204 khi xóa thành công, không có response body
        if (response.IsSuccessStatusCode)
        {
            return new NoteGroupDeleteApiResult
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

        return new NoteGroupDeleteApiResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }
}
