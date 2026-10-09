using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Infrastructure.Data;
using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public class AuthSessionRepository
    : RepositoryBase<AuthSession>, IAuthSessionRepository
{
     public AuthSessionRepository(BranchFlowDbContext context)
        : base(context)
    {
    }

    // Đọc phiên, tài khoản và role để Service kiểm tra tính hợp lệ
    public async Task<AuthSession?> GetByIdWithUserAsync(Guid sessionId)
    {
        return await Context.AuthSessions
            .AsNoTracking()
            .Include(session => session.User)
                .ThenInclude(user => user.Role)
            .SingleOrDefaultAsync(session => session.Id == sessionId);
    }

    // Lấy các phiên và token cần theo dõi để Service thu hồi
    public async Task<List<AuthSession>> GetNotRevokedByUserIdAsync(int userId)
    {
        return await Context.AuthSessions
            .Include(session => session.RefreshTokens)
            .Where(session =>
                session.UserId == userId
                && session.RevokedAt == null)
            .ToListAsync();
    }
}