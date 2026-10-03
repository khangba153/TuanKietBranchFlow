using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public interface IToppingGroupRepository : IRepositoryBase<ToppingGroup>
{
    // Lấy các nhóm chưa xóa cùng topping chưa xóa cho màn quản lý
    Task<List<ToppingGroup>> GetAllNotDeletedWithToppingsAsync();

    // Kiểm tra tên đã thuộc một nhóm topping chưa xóa hay chưa
    Task<bool> ExistsByNameAsync(string name);

    // Lấy nhóm chưa xóa cùng topping để cập nhật và trả dữ liệu đầy đủ
    Task<ToppingGroup?> GetNotDeletedByIdAsync(int toppingGroupId);

    // Kiểm tra tên trùng, loại trừ chính nhóm đang sửa
    Task<bool> ExistsByNameExceptIdAsync(string name, int toppingGroupId);
}