using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories.Common;

namespace TuanKietBranchFlow.Infrastructure.Repositories.Employees;

public interface IEmployeeTransferRepository : IRepositoryBase<EmployeeTransferRequest>
{
    Task<EmployeeTransferRequest?> GetPendingAsync(int employeeId);
    Task<int?> GetEmployeeIdAsync(int requestId);
    Task<List<EmployeeTransferRequest>> GetVisibleAsync(
        int userId,
        bool isOwner,
        bool incoming,
        int page,
        DateOnly today);
    Task InvalidatePendingAsync(
        int employeeId,
        string reason,
        DateTime now);
}
