using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public interface IRoleRepository : IRepositoryBase<Role>
{
    // Lấy danh sách các role chưa xóa
    Task<List<Role>> GetAllNotDeletedAsync();
}