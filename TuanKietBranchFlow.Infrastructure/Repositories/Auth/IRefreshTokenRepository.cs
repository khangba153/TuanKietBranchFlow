using TuanKietBranchFlow.Infrastructure.Repositories.Common;
using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories.Auth;

public interface IRefreshTokenRepository : IRepositoryBase<RefreshToken>
{
    // Tìm refresh token theo hash và lấy kèm phiên đăng nhập
    Task<RefreshToken?> GetByHashWithSessionAsync(string tokenHash);
}