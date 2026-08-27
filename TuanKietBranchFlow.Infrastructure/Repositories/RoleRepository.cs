using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Infrastructure.Data;
using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public class RoleRepository : RepositoryBase<Role>, IRoleRepository
{
    // Nhận DbContext từ DI
    public RoleRepository(BranchFlowDbContext context) : base(context)
    {
        
    }

    // Chỉ lấy những role chưa xóa
    public async Task<List<Role>> GetAllNotDeletedAsync()
    {
        return await Context.Roles
            .Where(role => !role.Deleted)
            .OrderBy(role => role.Id)
            .ToListAsync();
    }
}