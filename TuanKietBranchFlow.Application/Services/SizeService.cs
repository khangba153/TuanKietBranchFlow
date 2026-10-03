using TuanKietBranchFlow.Application.DTOs.MenuManagement;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;

namespace TuanKietBranchFlow.Application.Services;

public class SizeService : ISizeService
{
    private readonly ISizeRepository _sizeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SizeService(
        ISizeRepository sizeRepository,
        IUnitOfWork unitOfWork)
    {
        _sizeRepository = sizeRepository;
        _unitOfWork = unitOfWork;
    }

    // Chuyển các size chưa xóa thành dữ liệu dành cho API
    public async Task<List<MenuSizeDTO>> GetAllSizesAsync()
    {
        List<Size> sizes =
            await _sizeRepository.GetAllNotDeletedAsync();

        List<MenuSizeDTO> result = sizes
            .Select(size => new MenuSizeDTO
            {
                Id = size.Id,
                Name = size.Name,
                IsActive = size.IsActive
            })
            .ToList();

        return result;
    }

    // Chuẩn hóa tên, kiểm tra trùng và lưu Size mới
    public async Task<CreateSizeResultDTO> CreateSizeAsync(
        CreateSizeRequestDTO request)
    {
        CreateSizeResultDTO result = new CreateSizeResultDTO();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            result.IsNameInvalid = true;
            return result;
        }

        string sizeName = request.Name.Trim();

        if (sizeName.Length > 50)
        {
            result.IsNameInvalid = true;
            return result;
        }

        bool isNameDuplicated =
            await _sizeRepository.ExistsByNameAsync(sizeName);

        if (isNameDuplicated)
        {
            result.IsNameDuplicated = true;
            return result;
        }

        Size size = new Size
        {
            Name = sizeName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Deleted = false
        };

        await _sizeRepository.AddAsync(size);
        await _unitOfWork.SaveChangesAsync();

        result.Size = new MenuSizeDTO
        {
            Id = size.Id,
            Name = size.Name,
            IsActive = size.IsActive
        };

        return result;
    }

    // Kiểm tra dữ liệu, cập nhật Size đang được theo dõi và lưu thay đổi
    public async Task<UpdateSizeResultDTO> UpdateSizeAsync(
        int sizeId,
        UpdateSizeRequestDTO request)
    {
        UpdateSizeResultDTO result = new UpdateSizeResultDTO();

        // Tên phải hợp lệ và request phải gửi trạng thái IsActive.
        if (string.IsNullOrWhiteSpace(request.Name)
            || request.Name.Trim().Length > 50
            || !request.IsActive.HasValue)
        {
            result.IsRequestInvalid = true;
            return result;
        }

        string sizeName = request.Name.Trim();

        Size? size =
            await _sizeRepository.GetNotDeletedByIdAsync(sizeId);

        if (size == null)
        {
            result.IsSizeNotFound = true;
            return result;
        }

        bool isNameDuplicated =
            await _sizeRepository.ExistsByNameExceptIdAsync(
                sizeName,
                sizeId);

        if (isNameDuplicated)
        {
            result.IsNameDuplicated = true;
            return result;
        }

        // Size đang được DbContext theo dõi nên chỉ cần gán giá trị mới.
        size.Name = sizeName;
        size.IsActive = request.IsActive.Value;

        await _unitOfWork.SaveChangesAsync();

        result.Size = new MenuSizeDTO
        {
            Id = size.Id,
            Name = size.Name,
            IsActive = size.IsActive
        };

        return result;
    }

    // Đánh dấu Size đã xóa mềm và lưu thay đổi
    public async Task<DeleteSizeResultDTO> DeleteSizeAsync(int sizeId)
    {
        DeleteSizeResultDTO result = new DeleteSizeResultDTO();

        Size? size =
            await _sizeRepository.GetNotDeletedByIdAsync(sizeId);

        if (size == null)
        {
            result.IsSizeNotFound = true;
            return result;
        }

        size.Deleted = true;
        size.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        result.IsDeleted = true;
        return result;
    }
}