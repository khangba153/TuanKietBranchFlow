using TuanKietBranchFlow.Application.DTOs.Auth;

namespace TuanKietBranchFlow.Application.Services;

public interface IAuthService
{
    // Kiểm tra đăng nhập và trả token nếu thông tin hợp lệ
    Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO request);

    // Kiểm tra phiên và tài khoản tương ứng với thông tin trong JWT
    Task<bool> IsSessionValidAsync(
        Guid sessionId,
        int userId,
        string roleCode);

    // Thu hồi các phiên đăng nhập và refresh token của cùng tài khoản
    Task LogoutAllAsync(int userId);

    // Kiểm tra refresh token và cấp cặp token mới nếu hợp lệ.
    Task<LoginResponseDTO?> RefreshAsync(RefreshTokenRequestDTO request);
}