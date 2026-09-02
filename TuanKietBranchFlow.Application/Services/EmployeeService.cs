using TuanKietBranchFlow.Application.DTOs.Employees;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;

namespace TuanKietBranchFlow.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserBranchRepository _userBranchRepository;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    // Nhận các dependency cần dùng từ DI
    public EmployeeService(
        IEmployeeRepository employeeRepository,
        IBranchRepository branchRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUserBranchRepository userBranchRepository,
        IPasswordHasher<AppUser> passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _employeeRepository = employeeRepository;
        _branchRepository = branchRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userBranchRepository = userBranchRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
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

    // Tạo tài khoản, hồ sơ và phân công chi nhánh cho nhân viên mới
    public async Task<EmployeeCreateResultDTO> CreateEmployeeAsync(
        int currentAdminId,
        EmployeeCreateDTO request)
    {
        DateOnly currentDate = DateOnly.FromDateTime(DateTime.Today);

        // Bước 1: kiểm tra chi nhánh tồn tại và chưa bị xóa
        Branch? branch = await _branchRepository.GetNotDeletedByIdAsync(request.BranchId);

        if (branch == null)
        {
            return new EmployeeCreateResultDTO
            {
                IsBranchFound = false
            };
        }

        // Bước 2: kiểm tra ADMIN còn được phân công tại chi nhánh
        bool hasAccess =
            await _branchRepository.HasActiveAssignmentAsync(
                currentAdminId,
                request.BranchId,
                currentDate);

        if (!hasAccess)
        {
            return new EmployeeCreateResultDTO
            {
                IsBranchFound = true,
                HasAccess = false
            };
        }

        // Bước 3: tìm Role EMPLOYEE để gán cho tài khoản mới
        Role? employeeRole =
            await _roleRepository.SingleOrDefaultAsync(role =>
                role.Code == "EMPLOYEE" && !role.Deleted);

        if (employeeRole == null)
        {
            return new EmployeeCreateResultDTO
            {
                IsBranchFound = true,
                HasAccess = true,
                IsEmployeeRoleFound = false
            };
        }

        // Chuẩn hóa dữ liệu trước khi kiểm tra trùng và lưu
        string username = request.Username.Trim();
        string fullName = request.FullName.Trim();
        string employeeCode = request.EmployeeCode.Trim();
        string? email =
            string.IsNullOrWhiteSpace(request.Email)
                ? null
                : request.Email.Trim();
        string? phone =
            string.IsNullOrWhiteSpace(request.Phone)
                ? null
                : request.Phone.Trim();
        string? position =
            string.IsNullOrWhiteSpace(request.Position)
                ? null
                : request.Position.Trim();
        string? address =
            string.IsNullOrWhiteSpace(request.Address)
                ? null
                : request.Address.Trim();

        // Bước 4: Kiểm tra Username đã tồn tại hay chưa
        bool usernameExists = await _userRepository.UsernameExistsAsync(username);

        if (usernameExists)
        {
            return new EmployeeCreateResultDTO
            {
                IsBranchFound = true,
                HasAccess = true,
                IsEmployeeRoleFound = true,
                IsUsernameDuplicated = true
            };
        }

        // Email là tùy chọn nên chỉ kiểm tra khi có dữ liệu
        if (email != null)
        {
            bool emailExists = await _userRepository.EmailExistsAsync(email);

            if (emailExists)
            {
                return new EmployeeCreateResultDTO
                {
                    IsBranchFound = true,
                    HasAccess = true,
                    IsEmployeeRoleFound = true,
                    IsEmailDuplicated = true
                };
            }
        }

        // Bước 5: kiểm tra mã nhân viên đã tồn tại hay chưa
        bool employeeCodeExists =
            await _employeeRepository.EmployeeCodeExistsAsync(employeeCode);

        if (employeeCodeExists)
        {
            return new EmployeeCreateResultDTO
            {
                IsBranchFound = true,
                HasAccess = true,
                IsEmployeeRoleFound = true,
                IsEmployeeCodeDuplicated = true
            };
        }

        // Bước 6: tạo tài khoản đăng nhập cho nhân viên
        AppUser appUser = new AppUser
        {
            RoleId = employeeRole.Id,
            Username = username,
            PasswordHash = string.Empty,
            FullName = fullName,
            Email = email,
            Phone = phone,
            IsActive = true,
            Deleted = false
        };

        // Hash mật khẩu trước khi lưu
        appUser.PasswordHash = _passwordHasher.HashPassword(appUser, request.Password);

        // Bước 7: tạo hồ sơ nhân viên và liên kết với AppUser
        EmployeeProfile employeeProfile = new EmployeeProfile
        {
            User = appUser,
            EmployeeCode = employeeCode,
            DateOfBirth = request.DateOfBirth,
            HireDate = request.HireDate,
            Position = position,
            Address = address,
            BaseSalary = request.BaseSalary,
            Deleted = false
        };

        // Bước 8: tạo phân công chi nhánh đầu tiên
        UserBranch userBranch = new UserBranch
        {
            User = appUser,
            BranchId = request.BranchId,
            ActiveFrom = request.HireDate,
            ActiveTo = null
        };

        // Đánh dấu 3 Entity cần được thêm vào db
        await _userRepository.AddAsync(appUser);
        await _employeeRepository.AddAsync(employeeProfile);
        await _userBranchRepository.AddAsync(userBranch);

        // Lưu toàn bộ thay đổi
        await _unitOfWork.SaveChangesAsync();

        // Bước 9: tạo DTO trả về sau khi db đã sinh Id
        EmployeeDetailDTO employeeDTO = new EmployeeDetailDTO
        {
            Id = employeeProfile.Id,
            EmployeeCode = employeeProfile.EmployeeCode,
            FullName = appUser.FullName,
            DateOfBirth = employeeProfile.DateOfBirth,
            HireDate = employeeProfile.HireDate,
            Position = employeeProfile.Position,
            BaseSalary = employeeProfile.BaseSalary,
            Phone = appUser.Phone,
            Email = appUser.Email,
            Address = employeeProfile.Address,
            AvatarUrl = employeeProfile.AvatarUrl,
            BranchHistory = new List<EmployeeBranchHistoryDTO>
            {
                new EmployeeBranchHistoryDTO
                {
                    BranchId = branch.Id,
                    BranchName = branch.Name,
                    ActiveFrom = userBranch.ActiveFrom,
                    ActiveTo = userBranch.ActiveTo
                }
            }
        };

        return new EmployeeCreateResultDTO
        {
            IsBranchFound = true,
            HasAccess = true,
            IsEmployeeRoleFound = true,
            Employee = employeeDTO
        };
    }


}