using TuanKietBranchFlow.Application.DTOs.MenuManagement;

namespace TuanKietBranchFlow.Application.Services;

public interface ICategoryService
{
    // Lấy danh mục để ADMIN quản lý và OWNER xem
    Task<List<MenuCategoryDTO>> GetAllCategoriesAsync();

    // Tạo danh mục mới
    Task<CreateCategoryResultDTO> CreateCategoryAsync(
        CreateCategoryRequestDTO request);

    // Cập nhật tên và trạng thái hoạt động của danh mục
    Task<UpdateCategoryResultDTO> UpdateCategoryAsync(
        int categoryId,
        UpdateCategoryRequestDTO request);

    // Xóa mềm danh mục
    Task<DeleteCategoryResultDTO> DeleteCategoryAsync(int categoryId);
}
