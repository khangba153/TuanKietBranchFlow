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

    // Lấy 1 chi nhánh chưa xóa theo Id
    public async Task<Branch?> GetNotDeletedByIdAsync(int branchId)
    {
        return await Context.Branches
            .SingleOrDefaultAsync(branch =>
            branch.Id == branchId && !branch.Deleted);
    }

    // Kiêm trả phân công của người dùng tại 1 chi nhánh
    public async Task<bool> HasActiveAssignmentAsync(int userId, int branchId, DateOnly currentDate)
    {
        return await Context.UserBranches
        .AnyAsync(userBranch =>
           userBranch.UserId == userId
           && userBranch.BranchId == branchId
           && userBranch.ActiveFrom <= currentDate
           && (userBranch.ActiveTo == null || userBranch.ActiveTo >= currentDate));
    }
}