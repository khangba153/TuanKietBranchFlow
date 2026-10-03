using TuanKietBranchFlow.Application.DTOs.MenuManagement;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;

namespace TuanKietBranchFlow.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    // Chuyển danh mục chưa xóa thành dữ liệu dành cho API
    public async Task<List<MenuCategoryDTO>> GetAllCategoriesAsync()
    {
        List<Category> categories =
            await _categoryRepository.GetAllNotDeletedAsync();

        List<MenuCategoryDTO> result = categories
            .Select(category => new MenuCategoryDTO
            {
                Id = category.Id,
                Name = category.Name,
                IsActive = category.IsActive
            })
            .ToList();

        return result;
    }

    // Chuẩn hóa tên, kiểm tra trùng và lưu danh mục mới
    public async Task<CreateCategoryResultDTO> CreateCategoryAsync(
        CreateCategoryRequestDTO request)
    {
        CreateCategoryResultDTO result = new CreateCategoryResultDTO();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            result.IsNameInvalid = true;
            return result;
        }

        string categoryName = request.Name.Trim();

        if (categoryName.Length > 150)
        {
            result.IsNameInvalid = true;
            return result;
        }

        bool isNameDuplicated =
            await _categoryRepository.ExistsByNameAsync(categoryName);

        if (isNameDuplicated)
        {
            result.IsNameDuplicated = true;
            return result;
        }

        Category category = new Category
        {
            Name = categoryName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Deleted = false
        };

        await _categoryRepository.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        result.Category = new MenuCategoryDTO
        {
            Id = category.Id,
            Name = category.Name,
            IsActive = category.IsActive
        };

        return result;
    }

    // Kiểm tra dữ liệu, cập nhật danh mục đang được theo dõi và lưu thay đổi
    public async Task<UpdateCategoryResultDTO> UpdateCategoryAsync(
        int categoryId,
        UpdateCategoryRequestDTO request)
    {
        UpdateCategoryResultDTO result = new UpdateCategoryResultDTO();

        // Kiểm tra dữ liệu đầu vào
        if (string.IsNullOrWhiteSpace(request.Name)
            || request.Name.Trim().Length > 150
            || !request.IsActive.HasValue)
        {
            result.IsRequestInvalid = true;
            return result;
        }

        string categoryName = request.Name.Trim();

        Category? category =
            await _categoryRepository.GetNotDeletedByIdAsync(categoryId);

        if (category == null)
        {
            result.IsCategoryNotFound = true;
            return result;
        }

        bool isNameDuplicated =
            await _categoryRepository.ExistsByNameExceptIdAsync(
                categoryName,
                categoryId);

        if (isNameDuplicated)
        {
            result.IsNameDuplicated = true;
            return result;
        }

        // Entity được DbContext theo dõi chỉ cần gán giá trị mới
        category.Name = categoryName;
        category.IsActive = request.IsActive.Value;

        await _unitOfWork.SaveChangesAsync();

        result.Category = new MenuCategoryDTO
        {
            Id = category.Id,
            Name = category.Name,
            IsActive = category.IsActive
        };

        return result;
    }

    // Xóa mềm danh mục và lưu thay đổi
    public async Task<DeleteCategoryResultDTO> DeleteCategoryAsync(int categoryId)
    {
        DeleteCategoryResultDTO result = new DeleteCategoryResultDTO();

        Category? category =
            await _categoryRepository.GetNotDeletedByIdAsync(categoryId);

        if (category == null)
        {
            result.IsCategoryNotFound = true;
            return result;
        }

        category.Deleted = true;
        category.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        result.IsDeleted = true;
        return result;
    }
}
