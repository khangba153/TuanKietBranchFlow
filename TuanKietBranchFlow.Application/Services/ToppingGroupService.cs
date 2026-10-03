using TuanKietBranchFlow.Application.DTOs.MenuManagement;
using TuanKietBranchFlow.Infrastructure.Repositories;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;

namespace TuanKietBranchFlow.Application.Services;

public class ToppingGroupService : IToppingGroupService
{
    private readonly IToppingGroupRepository _toppingGroupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ToppingGroupService(
        IToppingGroupRepository toppingGroupRepository,
        IUnitOfWork unitOfWork)
    {
        _toppingGroupRepository = toppingGroupRepository;
        _unitOfWork = unitOfWork;
    }

    // Lấy entity từ Repository và ánh xạ thành DTO cho API
    public async Task<List<MenuToppingGroupDTO>> GetAllToppingGroupsAsync()
    {
        List<Infrastructure.Models.ToppingGroup> toppingGroups =
            await _toppingGroupRepository.GetAllNotDeletedWithToppingsAsync();

        List<MenuToppingGroupDTO> toppingGroupDTOs =
            new List<MenuToppingGroupDTO>();

        foreach (Infrastructure.Models.ToppingGroup toppingGroup in toppingGroups)
        {
            MenuToppingGroupDTO toppingGroupDTO = new MenuToppingGroupDTO
            {
                Id = toppingGroup.Id,
                Name = toppingGroup.Name,
                IsActive = toppingGroup.IsActive
            };

            foreach (Infrastructure.Models.Topping topping in toppingGroup.Toppings)
            {
                toppingGroupDTO.Toppings.Add(new MenuToppingDTO
                {
                    Id = topping.Id,
                    Name = topping.Name,
                    Price = topping.Price,
                    IsActive = topping.IsActive
                });
            }

            toppingGroupDTOs.Add(toppingGroupDTO);
        }

        return toppingGroupDTOs;
    }

    // Chuẩn hóa tên, kiểm tra trùng và lưu nhóm topping mới
    public async Task<CreateToppingGroupResultDTO> CreateToppingGroupAsync(
        CreateToppingGroupRequestDTO request)
    {
        CreateToppingGroupResultDTO result =
            new CreateToppingGroupResultDTO();

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
            await _toppingGroupRepository.ExistsByNameAsync(groupName);

        if (isNameDuplicated)
        {
            result.IsNameDuplicated = true;
            return result;
        }

        ToppingGroup toppingGroup = new ToppingGroup
        {
            Name = groupName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Deleted = false
        };

        await _toppingGroupRepository.AddAsync(toppingGroup);
        await _unitOfWork.SaveChangesAsync();

        result.ToppingGroup = new MenuToppingGroupDTO
        {
            Id = toppingGroup.Id,
            Name = toppingGroup.Name,
            IsActive = toppingGroup.IsActive
        };

        return result;
    }

    // Kiểm tra dữ liệu, cập nhật nhóm và trả thông tin sau khi lưu
    public async Task<UpdateToppingGroupResultDTO> UpdateToppingGroupAsync(
        int toppingGroupId,
        UpdateToppingGroupRequestDTO request)
    {
        UpdateToppingGroupResultDTO result =
            new UpdateToppingGroupResultDTO();

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

        ToppingGroup? toppingGroup =
            await _toppingGroupRepository.GetNotDeletedByIdAsync(
                toppingGroupId);

        if (toppingGroup == null)
        {
            result.IsToppingGroupNotFound = true;
            return result;
        }

        bool isNameDuplicated =
            await _toppingGroupRepository.ExistsByNameExceptIdAsync(
                groupName,
                toppingGroupId);

        if (isNameDuplicated)
        {
            result.IsNameDuplicated = true;
            return result;
        }

        // Entity đã được DbContext theo dõi nên chỉ cần gán giá trị mới
        toppingGroup.Name = groupName;
        toppingGroup.IsActive = request.IsActive.Value;

        await _unitOfWork.SaveChangesAsync();

        MenuToppingGroupDTO toppingGroupDTO = new MenuToppingGroupDTO
        {
            Id = toppingGroup.Id,
            Name = toppingGroup.Name,
            IsActive = toppingGroup.IsActive
        };

        // Giữ danh sách topping trong response để Web không mất dữ liệu con
        foreach (Topping topping in toppingGroup.Toppings)
        {
            toppingGroupDTO.Toppings.Add(new MenuToppingDTO
            {
                Id = topping.Id,
                Name = topping.Name,
                Price = topping.Price,
                IsActive = topping.IsActive
            });
        }

        result.ToppingGroup = toppingGroupDTO;

        return result;
    }

    // Đánh dấu nhóm đã xóa và lưu, không xóa topping con
    public async Task<DeleteToppingGroupResultDTO> DeleteToppingGroupAsync(
        int toppingGroupId)
    {
        DeleteToppingGroupResultDTO result =
            new DeleteToppingGroupResultDTO();

        ToppingGroup? toppingGroup =
            await _toppingGroupRepository.GetNotDeletedByIdAsync(
                toppingGroupId);

        if (toppingGroup == null)
        {
            result.IsToppingGroupNotFound = true;
            return result;
        }

        toppingGroup.Deleted = true;
        toppingGroup.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        result.IsDeleted = true;
        return result;
    }
}