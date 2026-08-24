using TuanKietBranchFlow.Application.DTOs.Auth;

namespace TuanKietBranchFlow.Application.Services;

public interface IAuthService
{
    // Kiểm tra đăng nhập và trả token nếu thông tin hợp lệ
    Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO request);
}