using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using TuanKietBranchFlow.Application.DTOs.MenuManagement;
using TuanKietBranchFlow.Infrastructure.Repositories;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;

namespace TuanKietBranchFlow.Application.Services;

public class NoteGroupService : INoteGroupService
{
    private readonly INoteGroupRepository _noteGroupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public NoteGroupService(
        INoteGroupRepository noteGroupRepository,
        IUnitOfWork unitOfWork)
    {
        _noteGroupRepository = noteGroupRepository;
        _unitOfWork = unitOfWork;
    }

    // Lấy entity từ Repository và ánh xạ thành DTO cho API
    public async Task<List<MenuNoteGroupDTO>> GetAllNoteGroupsAsync()
    {
        List<Infrastructure.Models.NoteGroup> noteGroups =
            await _noteGroupRepository.GetAllNotDeletedWithNoteOptionsAsync();

        List<MenuNoteGroupDTO> noteGroupDTOs =
            new List<MenuNoteGroupDTO>();

        foreach (Infrastructure.Models.NoteGroup noteGroup in noteGroups)
        {
            MenuNoteGroupDTO noteGroupDTO = new MenuNoteGroupDTO
            {
                Id = noteGroup.Id,
                Name = noteGroup.Name,
                IsActive = noteGroup.IsActive
            };

            foreach (Infrastructure.Models.NoteOption noteOption in noteGroup.NoteOptions)
            {
                noteGroupDTO.NoteOptions.Add(new MenuNoteOptionDTO
                {
                    Id = noteOption.Id,
                    Name = noteOption.Name,
                    IsActive = noteOption.IsActive
                });
            }

            noteGroupDTOs.Add(noteGroupDTO);
        }

        return noteGroupDTOs;
    }

    // Chuẩn hóa tên, kiểm tra trùng và lưu nhóm ghi chú mới
    public async Task<CreateNoteGroupResultDTO> CreateNoteGroupAsync(
        CreateNoteGroupRequestDTO request)
    {
        CreateNoteGroupResultDTO result =
            new CreateNoteGroupResultDTO();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            result.IsNameInvalid = true;
            return result;
        }

        string groupName = request.Name.Trim();

        if (groupName.Length > 100)
        {
            result.IsNameInvalid = true;
            return result;
        }

        bool isNameDuplicated =
            await _noteGroupRepository.ExistsByNameAsync(groupName);

        if (isNameDuplicated)
        {
            result.IsNameDuplicated = true;
            return result;
        }

        NoteGroup noteGroup = new NoteGroup
        {
            Name = groupName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Deleted = false
        };

        await _noteGroupRepository.AddAsync(noteGroup);
        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException sqlException
                  && (sqlException.Number == 2601 || sqlException.Number == 2627))
        {
            // Hai request đồng thời có thể vượt qua kiểm tra trùng trước khi lưu
            result.IsNameDuplicated = true;
            return result;
        }

        result.NoteGroup = new MenuNoteGroupDTO
        {
            Id = noteGroup.Id,
            Name = noteGroup.Name,
            IsActive = noteGroup.IsActive
        };

        return result;
    }

    // Kiểm tra dữ liệu, cập nhật nhóm và trả thông tin sau khi lưu
    public async Task<UpdateNoteGroupResultDTO> UpdateNoteGroupAsync(
        int noteGroupId,
        UpdateNoteGroupRequestDTO request)
    {
        UpdateNoteGroupResultDTO result =
            new UpdateNoteGroupResultDTO();

        if (string.IsNullOrWhiteSpace(request.Name)
            || !request.IsActive.HasValue)
        {
            result.IsRequestInvalid = true;
            return result;
        }

        string groupName = request.Name.Trim();

        if (groupName.Length > 100)
        {
            result.IsRequestInvalid = true;
            return result;
        }

        NoteGroup? noteGroup =
            await _noteGroupRepository.GetNotDeletedByIdAsync(
                noteGroupId);

        if (noteGroup == null)
        {
            result.IsNoteGroupNotFound = true;
            return result;
        }

        bool isNameDuplicated =
            await _noteGroupRepository.ExistsByNameExceptIdAsync(
                groupName,
                noteGroupId);

        if (isNameDuplicated)
        {
            result.IsNameDuplicated = true;
            return result;
        }

        // Entity đã được DbContext theo dõi nên chỉ cần gán giá trị mới
        noteGroup.Name = groupName;
        noteGroup.IsActive = request.IsActive.Value;
        noteGroup.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException sqlException
                  && (sqlException.Number == 2601 || sqlException.Number == 2627))
        {
            // Hai request đồng thời có thể vượt qua kiểm tra trùng trước khi lưu
            result.IsNameDuplicated = true;
            return result;
        }

        MenuNoteGroupDTO noteGroupDTO = new MenuNoteGroupDTO
        {
            Id = noteGroup.Id,
            Name = noteGroup.Name,
            IsActive = noteGroup.IsActive
        };

        // Giữ danh sách lựa chọn ghi chú trong response để Web không mất dữ liệu con
        foreach (NoteOption noteOption in noteGroup.NoteOptions)
        {
            noteGroupDTO.NoteOptions.Add(new MenuNoteOptionDTO
            {
                Id = noteOption.Id,
                Name = noteOption.Name,
                IsActive = noteOption.IsActive
            });
        }

        result.NoteGroup = noteGroupDTO;

        return result;
    }

    // Đánh dấu nhóm đã xóa và lưu, không xóa lựa chọn ghi chú con
    public async Task<DeleteNoteGroupResultDTO> DeleteNoteGroupAsync(
        int noteGroupId)
    {
        DeleteNoteGroupResultDTO result =
            new DeleteNoteGroupResultDTO();

        NoteGroup? noteGroup =
            await _noteGroupRepository.GetNotDeletedByIdAsync(
                noteGroupId);

        if (noteGroup == null)
        {
            result.IsNoteGroupNotFound = true;
            return result;
        }

        noteGroup.Deleted = true;
        noteGroup.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        result.IsDeleted = true;
        return result;
    }
}
