using TuanKietBranchFlow.Application.DTOs.MenuManagement;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;

namespace TuanKietBranchFlow.Application.Services;

public class ToppingService : IToppingService
{
    private readonly IToppingRepository _toppingRepository;
    private readonly IToppingGroupRepository _toppingGroupRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ToppingService(
        IToppingRepository toppingRepository,
        IToppingGroupRepository toppingGroupRepository,
        IBranchRepository branchRepository,
        IUnitOfWork unitOfWork)
    {
        _toppingRepository = toppingRepository;
        _toppingGroupRepository = toppingGroupRepository;
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
    }

    // Kiểm tra dữ liệu và nhóm, sau đó tạo topping mới
    public async Task<CreateToppingResultDTO> CreateToppingAsync(
        CreateToppingRequestDTO request)
    {
        CreateToppingResultDTO result = new CreateToppingResultDTO();

        if (request.ToppingGroupId <= 0
            || string.IsNullOrWhiteSpace(request.Name))
        {
            result.IsRequestInvalid = true;
            return result;
        }

        string toppingName = request.Name.Trim();

        // Giá phải vừa decimal(18,2), không âm và không bị làm tròn ngầm
        if (toppingName.Length > 150
            || request.Price <= 0
            || request.Price > 9999999999999999.99m
            || decimal.Round(request.Price, 2) != request.Price)
        {
            result.IsRequestInvalid = true;
            return result;
        }

        ToppingGroup? toppingGroup =
            await _toppingGroupRepository.GetNotDeletedByIdAsync(
                request.ToppingGroupId);

        if (toppingGroup == null)
        {
            result.IsToppingGroupNotFound = true;
            return result;
        }

        bool isNameDuplicated =
            await _toppingRepository.ExistsByNameAsync(toppingName);

        if (isNameDuplicated)
        {
            result.IsNameDuplicated = true;
            return result;
        }

        Topping topping = new Topping
        {
            ToppingGroupId = toppingGroup.Id,
            Name = toppingName,
            Price = request.Price,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Deleted = false
        };

        // Topping mới mặc định được bán tại các chi nhánh đang hoạt động
        List<Branch> activeBranches =
            await _branchRepository.GetActiveNotDeletedAsync();

        foreach (Branch branch in activeBranches)
        {
            BranchTopping branchTopping = new BranchTopping
            {
                BranchId = branch.Id,
                IsAvailable = true,
                UpdatedAt = topping.CreatedAt
            };

            topping.BranchToppings.Add(branchTopping);
        }

        // Lưu Topping và các liên kết chi nhánh cùng một lần
        await _toppingRepository.AddAsync(topping);
        await _unitOfWork.SaveChangesAsync();

        result.Topping = new MenuToppingDTO
        {
            Id = topping.Id,
            Name = topping.Name,
            Price = topping.Price,
            IsActive = topping.IsActive
        };

        return result;
    }

    // Kiểm tra dữ liệu, cập nhật topping và trả thông tin sau khi lưu
    public async Task<UpdateToppingResultDTO> UpdateToppingAsync(
        int toppingId,
        UpdateToppingRequestDTO request)
    {
        UpdateToppingResultDTO result = new UpdateToppingResultDTO();

        if (string.IsNullOrWhiteSpace(request.Name)
            || !request.IsActive.HasValue)
        {
            result.IsRequestInvalid = true;
            return result;
        }

        string toppingName = request.Name.Trim();

        if (toppingName.Length > 150
            || request.Price <= 0
            || request.Price > 9999999999999999.99m
            || decimal.Round(request.Price, 2) != request.Price)
        {
            result.IsRequestInvalid = true;
            return result;
        }

        Topping? topping =
            await _toppingRepository.GetNotDeletedByIdAsync(toppingId);

        if (topping == null)
        {
            result.IsToppingNotFound = true;
            return result;
        }

        bool isNameDuplicated =
            await _toppingRepository.ExistsByNameExceptIdAsync(
                toppingName,
                toppingId);

        if (isNameDuplicated)
        {
            result.IsNameDuplicated = true;
            return result;
        }

        // Entity được theo dõi; giữ nguyên nhóm và dữ liệu đơn hàng cũ
        topping.Name = toppingName;
        topping.Price = request.Price;
        topping.IsActive = request.IsActive.Value;
        topping.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        result.Topping = new MenuToppingDTO
        {
            Id = topping.Id,
            Name = topping.Name,
            Price = topping.Price,
            IsActive = topping.IsActive
        };

        return result;
    }

    // Đánh dấu topping đã xóa và lưu, không xóa dữ liệu liên quan
    public async Task<DeleteToppingResultDTO> DeleteToppingAsync(int toppingId)
    {
        DeleteToppingResultDTO result = new DeleteToppingResultDTO();

        Topping? topping =
            await _toppingRepository.GetNotDeletedByIdAsync(toppingId);

        if (topping == null)
        {
            result.IsToppingNotFound = true;
            return result;
        }

        topping.Deleted = true;
        topping.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        result.IsDeleted = true;
        return result;
    }
}