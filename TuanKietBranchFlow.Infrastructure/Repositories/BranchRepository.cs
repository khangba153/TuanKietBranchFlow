using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Infrastructure.Data;
using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public class BranchRepository : RepositoryBase<Branch>, IBranchRepository
{
    // Nhận DbContext từ DI và truyền cho RepositoryBase
    public BranchRepository(BranchFlowDbContext context) : base(context)
    {
    }

    // Lấy tất cả chi nhánh chưa bị xóa
    public async Task<List<Branch>> GetAllNotDeletedAsync()
    {
        return await Context.Branches.Where(branch => !branch.Deleted).ToListAsync();
    }

    // Lấy chi nhánh dựa theo phân công trong UserBranch
    public async Task<List<Branch>> GetAssignedBranchesAsync(int userId, DateOnly currentDate)
    {
        return await Context.Branches
            .Where(branch => !branch.Deleted
                && branch.UserBranches.Any(userBranch =>
                userBranch.UserId == userId
                && userBranch.ActiveFrom <= currentDate
                && (userBranch.ActiveTo == null || userBranch.ActiveTo >= currentDate)))
            .ToListAsync();
    }
}