using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public interface IUserBranchRepository : IRepositoryBase<UserBranch>
{
    // Lấy phân công còn hiệu lực của nhân viên tại 1 chi nhánh
    Task<UserBranch?> GetActiveAssignmentAsync(
        int userId,
        int branchId,
        DateOnly currentDate);
}