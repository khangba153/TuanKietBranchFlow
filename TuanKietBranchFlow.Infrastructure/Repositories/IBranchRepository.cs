using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public interface IBranchRepository : IRepositoryBase<Branch>
{
    // Lấy tất cả các chi nhánh chưa bị xóa
    Task<List<Branch>> GetAllNotDeletedAsync();

    // Lấy các chi nhánh đang được phân công chon 1 người dùng
    Task<List<Branch>> GetAssignedBranchesAsync(int userId, DateOnly currentDate);
}