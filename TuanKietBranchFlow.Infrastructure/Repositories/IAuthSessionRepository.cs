using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public interface IAuthSessionRepository : IRepositoryBase<AuthSession>
{
    // Lấy phiên và tài khoản để Service kiểm tra tính hợp lệ
    Task<AuthSession?> GetByIdWithUserAsync(Guid sessionId);

    // Lấy các phiên chưa thu hồi của tài khoản để đăng xuất tất cả
    Task<List<AuthSession>> GetNotRevokedByUserIdAsync(int userId);
}