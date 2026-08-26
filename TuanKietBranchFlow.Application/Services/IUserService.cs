using TuanKietBranchFlow.Application.DTOs.Users;

namespace TuanKietBranchFlow.Application.Services;

public interface IUserService
{
    // Lấy hồ sơ của người dùng đang đăng nhập.
    Task<UserProfileDTO?> GetCurrentProfileAsync(int userId);
}