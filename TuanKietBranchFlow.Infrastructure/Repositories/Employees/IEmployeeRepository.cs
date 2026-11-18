using TuanKietBranchFlow.Infrastructure.Repositories.Common;
using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories.Employees;

public interface IEmployeeRepository : IRepositoryBase<EmployeeProfile>
{
    // Lấy nhân viên thuộc 1 chi nhánh theo điều kiện tìm kiếm
    Task<List<EmployeeProfile>> GetByBranchAsync(
        int branchId,
        string keyword,
        bool? isActive,
        DateOnly currentDate);
    // Lấy chi tiết 1 nhân viên đang làm hoặc đã nghỉ tại chi nhánh được yêu cầu
    Task<EmployeeProfile?> GetDetailByIdAndBranchAsync(
        int employeeId,
        int branchId,
        DateOnly currentDate);
    // Kiểm tra mã nhân viên đã tồn tại hay chưa
    Task<bool> EmployeeCodeExistsAsync(string employeeCode);
    // Kiểm tra mã nhân viên có được hồ sơ khác sử dụng hay không
    Task<bool> EmployeeCodeExistsForOtherEmployeeAsync(
        string employeeCode,
        int currentEmployeeId);
    // Lấy nhân viên cùng tài khoản và lịch sử phân công, kể cả nhân viên đã nghỉ.
    Task<EmployeeProfile?> GetByIdWithUserAndBranchesAsync(int employeeId);
    // Giữ khóa ghi tài khoản đến cuối transaction để tuần tự hóa đổi trạng thái.
    Task<int> LockAccountForStatusChangeAsync(int employeeId);
}
