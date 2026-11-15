using TuanKietBranchFlow.Infrastructure.Repositories.Common;
using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories.Roles;

public interface IRoleRepository : IRepositoryBase<Role>
{
    // Lấy danh sách các role chưa xóa
    Task<List<Role>> GetAllNotDeletedAsync();
}