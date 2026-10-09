using Microsoft.AspNetCore.Identity;
using TuanKietBranchFlow.Application.DTOs.Auth;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;
using TuanKietBranchFlow.Application.Helpers;
using Microsoft.EntityFrameworkCore;

namespace TuanKietBranchFlow.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly JwtTokenService _jwtTokenService;

    // Quản lý phiên đăng nhập và refresh token
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    // Lưu các thay đổi của nghiệp vụ xác thực
    private readonly IUnitOfWork _unitOfWork;

    // Nhận các dependency phục vụ đăng nhập và quản lý token
    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher<AppUser> passwordHasher,
        JwtTokenService jwtTokenService,
        IAuthSessionRepository authSessionRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _authSessionRepository = authSessionRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
    }

    // Kiểm tra đăng nhập, lưu phiên và cấp cặp token
    public async Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO request)
    {
        // Chuẩn hóa username để tìm tài khoản
        string username = request.Username.Trim();

        AppUser? user =
            await _userRepository.GetByUsernameWithRoleAsync(username);

        if (user == null)
        {
            return null;
        }

        // Không cho tài khoản đã xóa hoặc bị khóa đăng nhập
        if (user.Deleted || !user.IsActive)
        {
            return null;
        }

        if (user.Role == null || user.Role.Deleted)
        {
            return null;
        }

        // Kiểm tra mật khẩu trước khi tạo phiên
        PasswordVerificationResult passwordResult =
            _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return null;
        }

        // Xác định thời hạn theo UTC: phiên 7 ngày, access token 15 phút
        DateTime now = DateTime.UtcNow;
        DateTime sessionExpiresAt = now.AddDays(7);
        DateTime accessTokenExpiresAt = now.AddMinutes(15);

        // Tạo phiên mới cho lần đăng nhập này
        AuthSession session = new AuthSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CreatedAt = now,
            ExpiresAt = sessionExpiresAt
        };

        // Chỉ lưu hash; token gốc được trả cho client
        string refreshToken = RefreshTokenHelper.GenerateToken();
        string tokenHash = RefreshTokenHelper.HashToken(refreshToken);

        RefreshToken refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Session = session,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = sessionExpiresAt
        };

        // Gắn JWT với đúng phiên vừa tạo
        string accessToken = _jwtTokenService.GenerateSessionToken(
            user.Id,
            user.Username,
            user.Role.Code,
            session.Id,
            accessTokenExpiresAt);

        // Chuẩn bị hai entity và lưu cùng một lần
        await _authSessionRepository.AddAsync(session);
        await _refreshTokenRepository.AddAsync(refreshTokenEntity);
        await _unitOfWork.SaveChangesAsync();

        // Chỉ trả cặp token khi dữ liệu đã lưu thành công
        return new LoginResponseDTO
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = accessTokenExpiresAt,
            RefreshTokenExpiresAt = sessionExpiresAt
        };
    }

    // Kiểm tra phiên và tài khoản trước khi chấp nhận JWT
    public async Task<bool> IsSessionValidAsync(
        Guid sessionId,
        int userId,
        string roleCode)
    {
        if (sessionId == Guid.Empty ||
            userId <= 0 ||
            string.IsNullOrWhiteSpace(roleCode))
        {
            return false;
        }

        AuthSession? session =
            await _authSessionRepository.GetByIdWithUserAsync(sessionId);

        // Phiên phải tồn tại và thuộc đúng tài khoản trong JWT
        if (session == null || session.UserId != userId)
        {
            return false;
        }

        // Phiên đã thu hồi hoặc hết hạn không được sử dụng
        if (session.RevokedAt != null ||
            session.ExpiresAt <= DateTime.UtcNow)
        {
            return false;
        }

        AppUser? user = session.User;

        if (user == null || user.Deleted || !user.IsActive)
        {
            return false;
        }

        // Không tiếp tục chấp nhận quyền cũ khi role đã thay đổi
        if (user.Role == null ||
            user.Role.Deleted ||
            user.Role.Code != roleCode)
        {
            return false;
        }

        return true;
    }

    // Thu hồi tất cả phiên, đọc lại nếu gặp xung đột khi lưu.
    public async Task LogoutAllAsync(int userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentException(
                "Id tài khoản không hợp lệ.",
                nameof(userId));
        }

        const int maxAttempts = 3;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            List<AuthSession> sessions =
                await _authSessionRepository
                    .GetNotRevokedByUserIdAsync(userId);

            if (sessions.Count == 0)
            {
                return;
            }

            DateTime now = DateTime.UtcNow;

            // Thu hồi phiên và token, không thay đổi lịch sử UsedAt.
            foreach (AuthSession session in sessions)
            {
                session.RevokedAt = now;

                foreach (RefreshToken refreshToken in session.RefreshTokens)
                {
                    if (refreshToken.RevokedAt == null)
                    {
                        refreshToken.RevokedAt = now;
                    }
                }
            }

            try
            {
                await _unitOfWork.SaveChangesAsync();
                return;
            }
            catch (DbUpdateConcurrencyException)
                when (attempt < maxAttempts)
            {
                // Bỏ state cũ để vòng tiếp theo đọc dữ liệu mới nhất.
                _unitOfWork.ClearTracking();
            }
        }
    }

    // Làm mới token và thu hồi các phiên khi phát hiện token bị dùng lại.
    public async Task<LoginResponseDTO?> RefreshAsync(
        RefreshTokenRequestDTO request)
    {
        if (request == null ||
            string.IsNullOrWhiteSpace(request.RefreshToken) ||
            request.RefreshToken.Length != 44)
        {
            return null;
        }

        // Đối chiếu đúng token gốc, không Trim hoặc đổi chữ hoa/thường.
        string tokenHash =
            RefreshTokenHelper.HashToken(request.RefreshToken);

        RefreshToken? currentToken =
            await _refreshTokenRepository
                .GetByHashWithSessionAsync(tokenHash);

        if (currentToken == null)
        {
            return null;
        }

        DateTime nowUtc = DateTime.UtcNow;
        AuthSession? session = currentToken.Session;

        // Token của phiên đã kết thúc không được ảnh hưởng phiên mới.
        if (session == null ||
            currentToken.RevokedAt != null ||
            currentToken.ExpiresAt <= nowUtc ||
            session.RevokedAt != null ||
            session.ExpiresAt <= nowUtc)
        {
            return null;
        }

        AppUser? user = session.User;

        if (user == null ||
            user.Deleted ||
            !user.IsActive ||
            user.Role == null ||
            user.Role.Deleted)
        {
            return null;
        }

        // Token đã dùng của phiên còn hiệu lực: thu hồi tất cả phiên.
        if (currentToken.UsedAt != null)
        {
            int userId = session.UserId;

            _unitOfWork.ClearTracking();
            await LogoutAllAsync(userId);

            return null;
        }

        try
        {
            return await RotateRefreshTokenAsync(currentToken, nowUtc);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Bỏ R1 đang sửa và R2 chưa lưu của lần rotation thất bại.
            _unitOfWork.ClearTracking();

            RefreshToken? latestToken =
                await _refreshTokenRepository
                    .GetByHashWithSessionAsync(tokenHash);

            DateTime latestNowUtc = DateTime.UtcNow;

            // Đọc lại để phân biệt reuse với token vừa bị logout/thu hồi.
            if (latestToken != null &&
                latestToken.Session != null &&
                latestToken.UsedAt != null &&
                latestToken.RevokedAt == null &&
                latestToken.ExpiresAt > latestNowUtc &&
                latestToken.Session.RevokedAt == null &&
                latestToken.Session.ExpiresAt > latestNowUtc)
            {
                int userId = latestToken.Session.UserId;

                _unitOfWork.ClearTracking();
                await LogoutAllAsync(userId);
            }

            // Không thử cấp thêm cặp token sau khi rotation gặp xung đột.
            return null;
        }
    }

    // Tạo cặp token trong bộ nhớ, không gia hạn phiên và chưa lưu database.
    private LoginResponseDTO CreateTokenResponse(
        AppUser user,
        AuthSession session,
        DateTime nowUtc)
    {
        if (nowUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Thời điểm hiện tại phải theo UTC.",
                nameof(nowUtc));
        }

        if (user.Role == null)
        {
            throw new InvalidOperationException(
                "Tài khoản chưa có thông tin quyền.");
        }

        // ExpiresAt được lưu theo UTC nhưng datetime2 không giữ DateTime.Kind.
        DateTime sessionExpiresAtUtc =
            DateTime.SpecifyKind(session.ExpiresAt, DateTimeKind.Utc);

        if (session.RevokedAt != null || sessionExpiresAtUtc <= nowUtc)
        {
            throw new InvalidOperationException(
                "Không thể cấp token cho phiên đã thu hồi hoặc hết hạn.");
        }

        // Access token tối đa 15 phút và không vượt thời hạn phiên.
        DateTime accessTokenExpiresAt = nowUtc.AddMinutes(15);

        if (accessTokenExpiresAt > sessionExpiresAtUtc)
        {
            accessTokenExpiresAt = sessionExpiresAtUtc;
        }

        string refreshToken = RefreshTokenHelper.GenerateToken();

        string accessToken = _jwtTokenService.GenerateSessionToken(
            user.Id,
            user.Username,
            user.Role.Code,
            session.Id,
            accessTokenExpiresAt);

        return new LoginResponseDTO
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = accessTokenExpiresAt,
            RefreshTokenExpiresAt = sessionExpiresAtUtc
        };
    }

    // Đánh dấu token cũ đã dùng và lưu token mới trong cùng một lần.
    private async Task<LoginResponseDTO> RotateRefreshTokenAsync(
        RefreshToken currentToken,
        DateTime nowUtc)
    {
        if (currentToken.UsedAt != null ||
            currentToken.RevokedAt != null ||
            currentToken.ExpiresAt <= nowUtc)
        {
            throw new InvalidOperationException(
                "Refresh token không còn hợp lệ để rotation.");
        }

        AuthSession session = currentToken.Session;
        AppUser user = session.User;

        // Tạo cặp token mới nhưng chưa gửi ra client.
        LoginResponseDTO response =
            CreateTokenResponse(user, session, nowUtc);

        RefreshToken newToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Session = session,
            TokenHash = RefreshTokenHelper.HashToken(response.RefreshToken),
            CreatedAt = nowUtc,
            ExpiresAt = response.RefreshTokenExpiresAt
        };

        // Entity token cũ đang được Repository truy vấn có tracking.
        currentToken.UsedAt = nowUtc;

        await _refreshTokenRepository.AddAsync(newToken);

        // Lưu UsedAt của R1 và thêm R2 cùng nhau.
        await _unitOfWork.SaveChangesAsync();

        return response;
    }
}
