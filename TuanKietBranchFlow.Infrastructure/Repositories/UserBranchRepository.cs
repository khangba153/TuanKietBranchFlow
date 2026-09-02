using TuanKietBranchFlow.Infrastructure.Data;
using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public class UserBranchRepository : RepositoryBase<UserBranch>, IUserBranchRepository
{
    // Nhận DbContext từ DI
    public UserBranchRepository(BranchFlowDbContext context) : base(context)
    {
        
    }
}