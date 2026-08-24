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
}
