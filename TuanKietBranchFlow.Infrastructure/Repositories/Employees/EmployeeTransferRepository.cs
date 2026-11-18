using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Infrastructure.Data;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories.Common;

namespace TuanKietBranchFlow.Infrastructure.Repositories.Employees;

public class EmployeeTransferRepository : RepositoryBase<EmployeeTransferRequest>, IEmployeeTransferRepository
{
    public EmployeeTransferRepository(BranchFlowDbContext context) : base(context)
    {
    }

    public Task<EmployeeTransferRequest?> GetPendingAsync(int employeeId)
    {
        return Context.EmployeeTransferRequests.SingleOrDefaultAsync(request => request.EmployeeId == employeeId && request.Status == "PENDING");
    }

    public Task<int?> GetEmployeeIdAsync(int requestId)
    {
        return Context.EmployeeTransferRequests.Where(request => request.Id == requestId).Select(request => (int?)request.EmployeeId).SingleOrDefaultAsync();
    }

    // Lọc phạm vi tại SQL, không tải yêu cầu ngoài quyền rồi mới lọc trong Web.
    public Task<List<EmployeeTransferRequest>> GetVisibleAsync(
        int userId,
        bool isOwner,
        bool incoming,
        int page,
        DateOnly today)
    {
        IQueryable<EmployeeTransferRequest> query = Context.EmployeeTransferRequests.AsNoTracking();
        if (!isOwner)
        {
            query = query.Where(request => Context.UserBranches.Any(assignment => assignment.UserId == userId && assignment.BranchId == (incoming ? request.TargetBranchId : request.SourceBranchId) && !assignment.Branch.Deleted && assignment.ActiveFrom <= today && (assignment.ActiveTo == null || assignment.ActiveTo >= today)));
        }

        return query.OrderByDescending(request => request.RequestedAt).ThenByDescending(request => request.Id).Skip((page - 1) * 20).Take(21).ToListAsync();
    }

    // Chỉ gọi khi đã giữ khóa nhân viên trong transaction đổi trạng thái.
    public Task InvalidatePendingAsync(
        int employeeId,
        string reason,
        DateTime now)
    {
        return Context.EmployeeTransferRequests.Where(request => request.EmployeeId == employeeId && request.Status == "PENDING").ExecuteUpdateAsync(setters => setters.SetProperty(request => request.Status, "INVALIDATED").SetProperty(request => request.ResolutionReason, reason).SetProperty(request => request.ProcessedAt, now));
    }
}
