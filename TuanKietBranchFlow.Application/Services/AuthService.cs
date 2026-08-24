using Microsoft.AspNetCore.Identity;
using TuanKietBranchFlow.Application.DTOs.Auth;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories;

namespace TuanKietBranchFlow.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly JwtTokenService _jwtTokenService;

    // Nhận cac dependency cần thiết từ DI
    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher<AppUser> passwordHasher,
        JwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    // Kiểm tra tài khoản, mật khẩu và tạo JWT
    public async Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO request)
    {
        // Username có thể loại bỏ khoảng trắng ở 2 đầu
        string username = request.Username.Trim();

        AppUser? user = await _userRepository.GetByUsernameWithRoleAsync(username);

        // Không cho tài khoản không tồn tại đăng nhập
        if (user == null)
        {
            return null;
        }
        // Không cho tài khoản đã xóa hoặc đang bị khóa
        if (user.Deleted || !user.IsActive)
        {
            return null;
        }

        // Role của tài khoản không còn hợp lệ
        if (user.Role == null || user.Role.Deleted)
        {
            return null;
        }

        // So sánh password người dùng đăng nhập với PasswordHash trong db
        PasswordVerificationResult passwordResult =
            _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);
        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return null;
        }

        // Chỉ tạo token sau khi toàn bộ điều kiện hợp lệ
        string accessToken = _jwtTokenService.GenerateToken(user.Id, user.Username, user.Role.Code);

        return new LoginResponseDTO
        {
            AccessToken = accessToken
        };
    }

}
