using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public interface IProductRepository : IRepositoryBase<Product>
{
    // Lấy danh sách món chưa bị xóa cho màn quản lý
    Task<List<Product>> GetAllNotDeletedAsync();

    // Kiểm tra tên đã thuộc một Product chưa xóa hay chưa
    Task<bool> ExistsByNameAsync(string name);

    // Lấy Product chưa xóa cùng các dữ liệu cần để cập nhật
    Task<Product?> GetNotDeletedByIdAsync(int productId);

    // Kiểm tra tên trùng nhưng bỏ qua Product đang sửa
    Task<bool> ExistsByNameExceptIdAsync(string name, int productId);
}