using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using TuanKietBranchFlow.Application.DTOs.MenuManagement;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;

namespace TuanKietBranchFlow.Application.Services;

public class NoteOptionService : INoteOptionService
{
    private readonly INoteOptionRepository _noteOptionRepository;
    private readonly INoteGroupRepository _noteGroupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public NoteOptionService(
        INoteOptionRepository noteOptionRepository,
        INoteGroupRepository noteGroupRepository,
        IUnitOfWork unitOfWork)
    {
        _noteOptionRepository = noteOptionRepository;
        _noteGroupRepository = noteGroupRepository;
        _unitOfWork = unitOfWork;
    }

    // Kiểm tra dữ liệu và nhóm, sau đó tạo lựa chọn ghi chú mới
    public async Task<CreateNoteOptionResultDTO> CreateNoteOptionAsync(
        CreateNoteOptionRequestDTO request)
    {
        CreateNoteOptionResultDTO result = new CreateNoteOptionResultDTO();

        if (request.NoteGroupId <= 0
            || string.IsNullOrWhiteSpace(request.Name))
        {
            result.IsRequestInvalid = true;
            return result;
        }

        string noteOptionName = request.Name.Trim();

        if (noteOptionName.Length > 100)
        {
            result.IsRequestInvalid = true;
            return result;
        }

        NoteGroup? noteGroup =
            await _noteGroupRepository.GetNotDeletedByIdAsync(
                request.NoteGroupId);

        if (noteGroup == null)
        {
            result.IsNoteGroupNotFound = true;
            return result;
        }

        bool isNameDuplicated =
            await _noteOptionRepository.ExistsByNameAsync(noteOptionName, noteGroup.Id);

        if (isNameDuplicated)
        {
            result.IsNameDuplicated = true;
            return result;
        }

        NoteOption noteOption = new NoteOption
        {
            NoteGroupId = noteGroup.Id,
            Name = noteOptionName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Deleted = false
        };

        await _noteOptionRepository.AddAsync(noteOption);
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

        result.NoteOption = new MenuNoteOptionDTO
        {
            Id = noteOption.Id,
            Name = noteOption.Name,
            IsActive = noteOption.IsActive
        };

        return result;
    }

    // Kiểm tra dữ liệu, cập nhật lựa chọn ghi chú và trả thông tin sau khi lưu
    public async Task<UpdateNoteOptionResultDTO> UpdateNoteOptionAsync(
        int noteOptionId,
        UpdateNoteOptionRequestDTO request)
    {
        UpdateNoteOptionResultDTO result = new UpdateNoteOptionResultDTO();

        if (string.IsNullOrWhiteSpace(request.Name)
            || !request.IsActive.HasValue)
        {
            result.IsRequestInvalid = true;
            return result;
        }

        string noteOptionName = request.Name.Trim();

        if (noteOptionName.Length > 100)
        {
            result.IsRequestInvalid = true;
            return result;
        }

        NoteOption? noteOption =
            await _noteOptionRepository.GetNotDeletedByIdAsync(noteOptionId);

        if (noteOption == null)
        {
            result.IsNoteOptionNotFound = true;
            return result;
        }

        bool isNameDuplicated =
            await _noteOptionRepository.ExistsByNameExceptIdAsync(
                noteOptionName,
                noteOptionId,
                noteOption.NoteGroupId);

        if (isNameDuplicated)
        {
            result.IsNameDuplicated = true;
            return result;
        }

        // Entity được theo dõi; giữ nguyên nhóm và dữ liệu đơn hàng cũ
        noteOption.Name = noteOptionName;
        noteOption.IsActive = request.IsActive.Value;
        noteOption.UpdatedAt = DateTime.UtcNow;

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

        result.NoteOption = new MenuNoteOptionDTO
        {
            Id = noteOption.Id,
            Name = noteOption.Name,
            IsActive = noteOption.IsActive
        };

        return result;
    }

    // Đánh dấu lựa chọn ghi chú đã xóa và lưu, không xóa dữ liệu liên quan
    public async Task<DeleteNoteOptionResultDTO> DeleteNoteOptionAsync(int noteOptionId)
    {
        DeleteNoteOptionResultDTO result = new DeleteNoteOptionResultDTO();

        NoteOption? noteOption =
            await _noteOptionRepository.GetNotDeletedByIdAsync(noteOptionId);

        if (noteOption == null)
        {
            result.IsNoteOptionNotFound = true;
            return result;
        }

        noteOption.Deleted = true;
        noteOption.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        result.IsDeleted = true;
        return result;
    }
}
