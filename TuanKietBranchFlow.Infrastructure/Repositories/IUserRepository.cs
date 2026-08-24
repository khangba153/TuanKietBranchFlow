using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public interface IUserRepository : IRepositoryBase<AppUser>
{
    // Tìm tài khoản theo username và lấy kèm role
    Task<AppUser?> GetByUsernameWithRoleAsync(string username);
}