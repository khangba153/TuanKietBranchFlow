using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public interface IUserRepository : IRepositoryBase<AppUser>
{
    // Tìm tài khoản theo username và lấy kèm role
    Task<AppUser?> GetByUsernameWithRoleAsync(string username);

    // Lấy hồ sơ người dùng và các dữ liệu liên quan theo Id
    Task<AppUser?> GetProfileByIdAsync(int userId);
}