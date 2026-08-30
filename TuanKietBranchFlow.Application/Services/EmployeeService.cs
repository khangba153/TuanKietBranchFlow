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

    // Lấy chi tiết nhân viên theo chi nhánh và quyền của người dùng hiện tại
    public async Task<EmployeeDetailResultDTO> GetEmployeeDetailAsync(
        int currentUserId,
        string currentUserRole,
        int employeeId,
        int branchId)
    {
        // Lấy ngày hiện tại để kiểm tra thời hạn phân công
        DateOnly currentDate = DateOnly.FromDateTime(DateTime.Today);

        // Bước 1: Kiểm tra chi nhánh tồn tại và chưa bị xóa
        Branch? branch = await _branchRepository.GetNotDeletedByIdAsync(branchId);

        if (branch == null)
        {
            return new EmployeeDetailResultDTO
            {
                IsBranchFound = false,
                HasAccess = false,
                IsEmployeeFound = false
            };
        }

        // Bước 2: Kiểm tra người dùng hiện tại có quyền xem chi nhánh hay không
        bool hasAccess = false;

        // OWNER được phép xem nhân viên của tất cả các chi nhánh
        if (currentUserRole == "OWNER")
        {
            hasAccess = true;
        }
        // ADMIN chỉ được xem chi nhánh có phân công còn hiệu lực
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
            return new EmployeeDetailResultDTO
            {
                IsBranchFound = true,
                HasAccess = false,
                IsEmployeeFound = false
            };
        }

        // Lấy hồ sơ nhân viên thuộc chi nhánh được yêu cầu
        EmployeeProfile? employeeProfile =
            await _employeeRepository.GetDetailByIdAndBranchAsync(
                employeeId,
                branchId,
                currentDate);

        if (employeeProfile == null)
        {
            return new EmployeeDetailResultDTO
            {
                IsBranchFound = true,
                HasAccess = true,
                IsEmployeeFound = false   
            };
        }

        // Bước 4: chuyển lịch phân công chi nhánh sang DTO
        List<EmployeeBranchHistoryDTO> branchHistory =
            new List<EmployeeBranchHistoryDTO>();
        
        foreach (UserBranch userBranch in employeeProfile.User.UserBranches)
        {
            // Không hiển thị chi nhánh đã xóa
            if (userBranch.Branch.Deleted)
            {
                continue;
            }

            EmployeeBranchHistoryDTO branchHistoryDTO =
                new EmployeeBranchHistoryDTO
                {
                    BranchId = userBranch.BranchId,
                    BranchName = userBranch.Branch.Name,
                    ActiveFrom = userBranch.ActiveFrom,
                    ActiveTo = userBranch.ActiveTo
                };
            
            branchHistory.Add(branchHistoryDTO);
        }
        
        // Bước 5: chuyển Entity thành DTO dành cho màn hình chi tiết
        EmployeeDetailDTO employeeDTO = 
            new EmployeeDetailDTO
            {
                Id = employeeProfile.Id,
                EmployeeCode = employeeProfile.EmployeeCode,
                FullName = employeeProfile.User.FullName,
                DateOfBirth = employeeProfile.DateOfBirth,
                HireDate = employeeProfile.HireDate,
                Position = employeeProfile.Position,
                BaseSalary = employeeProfile.BaseSalary,
                Phone = employeeProfile.User.Phone,
                Email = employeeProfile.User.Email,
                Address = employeeProfile.Address,
                AvatarUrl = employeeProfile.AvatarUrl,
                BranchHistory = branchHistory
            };
        
        // Tất cả điều kiện hợp lệ mới trả dữ liệu
        return new EmployeeDetailResultDTO
        {
            IsBranchFound = true,
            HasAccess = true,
            IsEmployeeFound = true,
            Employee = employeeDTO
        };
    }


    
}