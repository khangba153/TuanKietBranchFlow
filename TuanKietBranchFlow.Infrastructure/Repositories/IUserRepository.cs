using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public interface IUserRepository : IRepositoryBase<AppUser>
{
    // Tìm tài khoản theo username và lấy kèm role
    Task<AppUser?> GetByUsernameWithRoleAsync(string username);

    // Lấy hồ sơ người dùng và các dữ liệu liên quan theo Id
    Task<AppUser?> GetProfileByIdAsync(int userId);
    
    // Kiểm tra Username đã được sử dụng hay chưa
    Task<bool> UsernameExistsAsync(string username);

    // Kiểm tra email đã tồn tại hay chưa
    Task<bool> EmailExistsAsync(string email);
}