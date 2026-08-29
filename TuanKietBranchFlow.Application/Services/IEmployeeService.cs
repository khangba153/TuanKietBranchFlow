using TuanKietBranchFlow.Application.DTOs.Employees;

namespace TuanKietBranchFlow.Application.Services;

public interface IEmployeeService
{
    // Lấy danh sách nhân viên theo chi nhánh và quyền của người dùng hiện tại
    Task<EmployeeListResultDTO> GetEmployeesByBranchAsync(
        int currentUserId,
        string currentUserRole,
        int branchId,
        string keyword,
        bool? isActive);
}