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
    
    // Lấy chi tiết nhân viên theo quyền truy cập chi nhánh hiện tại
    Task<EmployeeDetailResultDTO> GetEmployeeDetailAsync(
        int currentUserId,
        string currentUserRole,
        int employeeId,
        int branchId);

    // Tạo tài khoản, hồ sơ và phân công chi nhánh cho nhân viên mới
    Task<EmployeeCreateResultDTO> CreateEmployeeAsync(
        int currentAdminId,
        EmployeeCreateDTO request);

    // Cập nhật thông tin nhân viên tại chi nhánh ADMIN được phân công
    Task<EmployeeUpdateResultDTO> UpdateEmployeeAsync(
        int currentAdminId,
        int employeeId,
        int branchId,
        EmployeeUpdateDTO request);

    // Chuyển nhân viên từ chi nhánh hiện tại sang chi nhánh mới
    Task<EmployeeChangeBranchResultDTO> ChangeEmployeeBranchAsync(
        int currentAdminId,
        int employeeId,
        int currentBranchId,
        EmployeeChangeBranchDTO request);
}