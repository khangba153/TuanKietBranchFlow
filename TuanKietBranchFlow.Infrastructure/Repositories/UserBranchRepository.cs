using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Infrastructure.Data;
using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public class UserBranchRepository : RepositoryBase<UserBranch>, IUserBranchRepository
{
    // Nhận DbContext từ DI
    public UserBranchRepository(BranchFlowDbContext context) : base(context)
    {
    }

    // Lấy phân công của nhân viên còn hiệu lực tại ngày được truyền vào
    public async Task<UserBranch?> GetActiveAssignmentAsync(
        int userId,
        int branchId,
        DateOnly currentDate)
    {
        return await Context.UserBranches
            .SingleOrDefaultAsync(userBranch =>
                userBranch.UserId == userId
                && userBranch.BranchId == branchId
                && userBranch.ActiveFrom <= currentDate
                && (userBranch.ActiveTo == null
                    || userBranch.ActiveTo >= currentDate));
    }
}