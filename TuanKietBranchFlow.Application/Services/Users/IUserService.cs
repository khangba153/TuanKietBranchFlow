using TuanKietBranchFlow.Application.DTOs.Users;

namespace TuanKietBranchFlow.Application.Services.Users;

public interface IUserService
{
    // Lấy hồ sơ của người dùng đang đăng nhập.
    Task<UserProfileDTO?> GetCurrentProfileAsync(int userId);
}
