using TuanKietBranchFlow.Infrastructure.Repositories.Common;
using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Infrastructure.Data;
using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories.Auth;

public class AuthSessionRepository : RepositoryBase<AuthSession>, IAuthSessionRepository
{
    public AuthSessionRepository(BranchFlowDbContext context) : base(context)
    {
    }

    // Thu hồi cả phiên và token trong transaction đổi trạng thái nhân viên.
    public async Task RevokeAllForEmployeeStatusAsync(
        int userId,
        DateTime revokedAt)
    {
        await Context.AuthSessions.Where(session => session.UserId == userId && session.RevokedAt == null).ExecuteUpdateAsync(setters => setters.SetProperty(session => session.RevokedAt, revokedAt));
        await Context.RefreshTokens.Where(token => token.Session.UserId == userId && token.RevokedAt == null).ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, revokedAt));
    }

    // Đọc phiên, tài khoản và role để Service kiểm tra tính hợp lệ
    public async Task<AuthSession?> GetByIdWithUserAsync(Guid sessionId)
    {
        return await Context.AuthSessions.AsNoTracking().Include(session => session.User).ThenInclude(user => user.Role).SingleOrDefaultAsync(session => session.Id == sessionId);
    }

    // Lấy các phiên và token cần theo dõi để Service thu hồi
    public async Task<List<AuthSession>> GetNotRevokedByUserIdAsync(int userId)
    {
        return await Context.AuthSessions.Include(session => session.RefreshTokens).Where(session => session.UserId == userId && session.RevokedAt == null).ToListAsync();
    }
}
