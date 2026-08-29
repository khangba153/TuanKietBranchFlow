using TuanKietBranchFlow.Application.DTOs.Employees;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories;

namespace TuanKietBranchFlow.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IBranchRepository _branchRepository;

    // Nhận các Repository cần dùng từ DI
    public EmployeeService(IEmployeeRepository employeeRepository, IBranchRepository branchRepository)
    {
        _employeeRepository = employeeRepository;
        _branchRepository = branchRepository;
    }

    // Lấy danh sách nhân viên sau khi kiểm tra tra nhánh và quyền truy cập
    public async Task<EmployeeListResultDTO> GetEmployeesByBranchAsync(
        int currentUserId,
        string currentUserRole,
        int branchId,
        string keyword,
        bool? isActive)
    {
        DateOnly currentDate = DateOnly.FromDateTime(DateTime.Today);

        // Bước 1: kiểm tra chi nhánh có tồn tại và chưa bị xóa
        Branch? branch = await _branchRepository.GetNotDeletedByIdAsync(branchId);

        if (branch == null)
        {
            return new EmployeeListResultDTO
            {
                IsBranchFound = false,
                HasAccess = false
            };
        }

        // Bước 2: kiểm tra người dùng có quyền truy cập chi nhánh
        bool hasAccess = false;

        // OWNER được xem nhân viên của tất cả các chi nhánh
        if (currentUserRole == "OWNER")
        {
            hasAccess = true;
        }

        // ADMIN phải có phân công còn hiệu lực tại chi nhánh
        else if (currentUserRole == "ADMIN")
        {
            hasAccess = 
                await _branchRepository.HasActiveAssignmentAsync(
                    currentUserId,
                    branchId,
                    currentDate);
        }

        if (!hasAccess)
        {
            return new EmployeeListResultDTO
            {
                IsBranchFound = true,
                HasAccess = false
            };
        }

        // Bước 3: Lấy Entity nhân viên từ Repository
        List<EmployeeProfile> employeeProfiles =
            await _employeeRepository.GetByBranchAsync(
                branchId,
                keyword,
                isActive,
                currentDate);

        // Chuyển entity thành DTO dùng cho giao diện
        List<EmployeeListItemDTO> employees = new List<EmployeeListItemDTO>();

        foreach (EmployeeProfile employee in employeeProfiles)
        {
            EmployeeListItemDTO employeeDTO =
                new EmployeeListItemDTO
                {
                    Id = employee.Id,
                    FullName = employee.User.FullName,
                    EmployeeCode = employee.EmployeeCode,
                    Position = employee.Position,
                    BranchId = branch.Id,
                    BranchName = branch.Name,
                    HireDate = employee.HireDate,
                    IsActive = employee.User.IsActive,
                    AvatarUrl = employee.AvatarUrl
                };
            
            employees.Add(employeeDTO);
        }


        return new EmployeeListResultDTO
        {
            IsBranchFound = true,
            HasAccess = true,
            Employees = employees
        };
    }

    
}