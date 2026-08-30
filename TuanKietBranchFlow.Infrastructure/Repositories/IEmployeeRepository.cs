using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public interface IEmployeeRepository : IRepositoryBase<EmployeeProfile>
{
    // Lấy nhân viên thuộc 1 chi nhánh theo điều kiện tìm kiếm
    Task<List<EmployeeProfile>> GetByBranchAsync(int branchId, string keyword, bool? isActive, DateOnly currentDate);

    // Lấy chi tiết 1 nhân viên nếu đang thuộc chi nhánh được yêu cầu
    Task<EmployeeProfile?> GetDetailByIdAndBranchAsync(int employeeId, int branchId, DateOnly currentDate);
}