using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public interface ICategoryRepository : IRepositoryBase<Category>
{
    // Lấy danh mục chưa bị xóa cho màn quản lý
    Task<List<Category>> GetAllNotDeletedAsync();

    // Kiểm tra tên đã thuộc một danh mục chưa xóa hay chưa
    Task<bool> ExistsByNameAsync(string name);

    // Lấy danh mục chưa xóa theo Id để Service cập nhật
    Task<Category?> GetNotDeletedByIdAsync(int categoryId);

    // Kiểm tra tên trùng, bỏ qua chính danh mục đang sửa
    Task<bool> ExistsByNameExceptIdAsync(string name, int categoryId);
}