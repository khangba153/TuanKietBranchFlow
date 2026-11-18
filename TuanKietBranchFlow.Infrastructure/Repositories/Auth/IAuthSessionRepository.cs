using TuanKietBranchFlow.Infrastructure.Repositories.Common;
using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories.Auth;

public interface IAuthSessionRepository : IRepositoryBase<AuthSession>
{
    // Lấy phiên và tài khoản để Service kiểm tra tính hợp lệ
    Task<AuthSession?> GetByIdWithUserAsync(Guid sessionId);
    // Lấy các phiên chưa thu hồi của tài khoản để đăng xuất tất cả
    Task<List<AuthSession>> GetNotRevokedByUserIdAsync(int userId);
    // Thu hồi cả phiên và token trong transaction của nghiệp vụ nhân viên.
    Task RevokeAllForEmployeeStatusAsync(
        int userId,
        DateTime revokedAt);
}
