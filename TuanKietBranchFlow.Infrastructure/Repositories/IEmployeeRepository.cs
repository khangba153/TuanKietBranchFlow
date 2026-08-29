using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public interface IEmployeeRepository : IRepositoryBase<EmployeeProfile>
{
    // Lấy nhân viên thuộc 1 chi nhánh theo điều kiện tìm kiếm
    Task<List<EmployeeProfile>> GetByBranchAsync(int branchId, string keyword, bool? isActive, DateOnly currentDate);
}