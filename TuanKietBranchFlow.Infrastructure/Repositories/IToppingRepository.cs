using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public interface IToppingRepository : IRepositoryBase<Topping>
{
    // Kiểm tra tên topping chưa xóa trên toàn hệ thống
    Task<bool> ExistsByNameAsync(string name);

    // Lấy topping còn thuộc nhóm chưa xóa để cập nhật
    Task<Topping?> GetNotDeletedByIdAsync(int toppingId);

    // Kiểm tra tên trùng, loại chính topping đang sửa
    Task<bool> ExistsByNameExceptIdAsync(string name, int toppingId);
}