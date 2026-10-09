using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Infrastructure.Data;
using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public class RefreshTokenRepository
    : RepositoryBase<RefreshToken>, IRefreshTokenRepository
{
    // Nhận DbContext dùng chung với các Repository và Unit of Work
    public RefreshTokenRepository(BranchFlowDbContext context)
        : base(context)
    {
    }

    // Lấy token và dữ liệu liên quan để Service kiểm tra, cập nhật
    public async Task<RefreshToken?> GetByHashWithSessionAsync(string tokenHash)
    {
        return await Context.RefreshTokens
            .Include(token => token.Session)
                .ThenInclude(session => session.User)
                    .ThenInclude(user => user.Role)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash);
    }
}