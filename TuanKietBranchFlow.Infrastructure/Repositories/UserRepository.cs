using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Data;


namespace TuanKietBranchFlow.Infrastructure.Repositories;

public class UserRepository : RepositoryBase<AppUser>, IUserRepository
{
    // Nhận DbContext từ DI và truyền cho RepositoryBase
    public UserRepository(BranchFlowDbContext context) : base(context)
    {

    }

    // Lấy tài khoản và role trong cùng 1 truy vấn db
    public async Task<AppUser?> GetByUsernameWithRoleAsync(string username)
    {
        return await Context.AppUsers
            .Include(user => user.Role)
            .SingleOrDefaultAsync(user => user.Username == username);
    }

    // Lấy thông tin tài khoản, hồ sơ nhân viên, Role và phân công chi nhánh
    public async Task<AppUser?> GetProfileByIdAsync(int userId)
    {
        return await Context.AppUsers
            .Include(user => user.Role)
            .Include(user => user.EmployeeProfile)
            .Include(user => user.UserBranches).ThenInclude(userBranch => userBranch.Branch)
            .SingleOrDefaultAsync(user => user.Id == userId && user.IsActive && !user.Deleted);
    }
}
