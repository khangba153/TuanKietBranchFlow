using TuanKietBranchFlow.Infrastructure.Repositories.Branches;
using TuanKietBranchFlow.Infrastructure.Repositories.Employees;
using TuanKietBranchFlow.Infrastructure.Repositories.Roles;
using TuanKietBranchFlow.Infrastructure.Repositories.Users;
using TuanKietBranchFlow.Application.DTOs.Employees;
using TuanKietBranchFlow.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;
using TuanKietBranchFlow.Infrastructure.Repositories.Auth;
using TuanKietBranchFlow.Application.DTOs.Employees.Transfers;
using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Infrastructure.Repositories.Common;

namespace TuanKietBranchFlow.Application.Services.Employees;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserBranchRepository _userBranchRepository;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly IEmployeeTransferRepository _employeeTransferRepository;
    private readonly IEmployeeTransferService _employeeTransferService;
    // Nhận các dependency cần dùng từ DI
    public EmployeeService(
        IEmployeeRepository employeeRepository,
        IBranchRepository branchRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUserBranchRepository userBranchRepository,
        IPasswordHasher<AppUser> passwordHasher,
        IUnitOfWork unitOfWork,
        IAuthSessionRepository authSessionRepository,
        IEmployeeTransferRepository transferRepository,
        IEmployeeTransferService transferService)
    {
        _employeeRepository = employeeRepository;
        _branchRepository = branchRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userBranchRepository = userBranchRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _authSessionRepository = authSessionRepository;
        _employeeTransferRepository = transferRepository;
        _employeeTransferService = transferService;
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
            hasAccess = await _branchRepository.HasActiveAssignmentAsync(currentUserId, branchId, currentDate);
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
        List<EmployeeProfile> employeeProfiles = await _employeeRepository.GetByBranchAsync(branchId, keyword, isActive, currentDate);
        // Chuyển entity thành DTO dùng cho giao diện
        List<EmployeeListItemDTO> employees = new List<EmployeeListItemDTO>();
        foreach (EmployeeProfile employee in employeeProfiles)
        {
            EmployeeListItemDTO employeeDTO = new EmployeeListItemDTO

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
            hasAccess = await _branchRepository.HasActiveAssignmentAsync(currentUserId, branchId, currentDate);
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
        EmployeeProfile? employeeProfile = await _employeeRepository.GetDetailByIdAndBranchAsync(employeeId, branchId, currentDate);
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
        List<EmployeeBranchHistoryDTO> branchHistory = new List<EmployeeBranchHistoryDTO>();
        foreach (UserBranch userBranch in employeeProfile.User.UserBranches)
        {
            // Không hiển thị chi nhánh đã xóa
            if (userBranch.Branch.Deleted)
            {
                continue;
            }

            EmployeeBranchHistoryDTO branchHistoryDTO = new EmployeeBranchHistoryDTO

            {

                BranchId = userBranch.BranchId,
                BranchName = userBranch.Branch.Name,
                ActiveFrom = userBranch.ActiveFrom,
                ActiveTo = userBranch.ActiveTo

            };
            branchHistory.Add(branchHistoryDTO);
        }

        // Bước 5: chuyển Entity thành DTO dành cho màn hình chi tiết
        EmployeeDetailDTO employeeDTO = new EmployeeDetailDTO

        {

            Id = employeeProfile.Id,
            EmployeeCode = employeeProfile.EmployeeCode,
            FullName = employeeProfile.User.FullName,
            DateOfBirth = employeeProfile.DateOfBirth,
            HireDate = employeeProfile.HireDate,
            IsActive = employeeProfile.User.IsActive,
            LeaveDate = employeeProfile.LeaveDate,
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
        // MVP chưa hỗ trợ nhân viên bắt đầu làm trong tương lai
        if (request.HireDate > currentDate)
        {
            return new EmployeeCreateResultDTO

            {

                IsHireDateInFuture = true

            };
        }

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
        bool hasAccess = await _branchRepository.HasActiveAssignmentAsync(currentAdminId, request.BranchId, currentDate);
        if (!hasAccess)
        {
            return new EmployeeCreateResultDTO

            {

                IsBranchFound = true,
                HasAccess = false

            };
        }

        // Bước 3: tìm Role EMPLOYEE để gán cho tài khoản mới
        Role? employeeRole = await _roleRepository.SingleOrDefaultAsync(role => role.Code == "EMPLOYEE" && !role.Deleted);
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
        string? email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        string? phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        string? position = string.IsNullOrWhiteSpace(request.Position) ? null : request.Position.Trim();
        string? address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
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
        bool employeeCodeExists = await _employeeRepository.EmployeeCodeExistsAsync(employeeCode);
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

        if (!branch.IsActive)
        {
            return new EmployeeCreateResultDTO

            {

                IsBranchFound = true,
                HasAccess = true,
                IsEmployeeRoleFound = true,
                IsBranchInactive = true

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
        // Unique index là lớp bảo vệ cuối khi hai request vượt qua kiểm tra Exists cùng lúc.
        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (DatabaseConflict.IsUniqueViolation(exception))
        {
            _unitOfWork.ClearTracking();
            bool usernameDuplicated = await _userRepository.UsernameExistsAsync(username);
            bool emailDuplicated = email != null && await _userRepository.EmailExistsAsync(email);
            bool codeDuplicated = await _employeeRepository.EmployeeCodeExistsAsync(employeeCode);
            if (!usernameDuplicated && !emailDuplicated && !codeDuplicated)
            {
                throw;
            }

            return new EmployeeCreateResultDTO

            {

                IsBranchFound = true,
                HasAccess = true,
                IsEmployeeRoleFound = true,
                IsUsernameDuplicated = usernameDuplicated,
                IsEmailDuplicated = emailDuplicated,
                IsEmployeeCodeDuplicated = codeDuplicated

            };
        }

        // Bước 9: tạo DTO trả về sau khi db đã sinh Id
        EmployeeDetailDTO employeeDTO = new EmployeeDetailDTO

        {

            Id = employeeProfile.Id,
            EmployeeCode = employeeProfile.EmployeeCode,
            FullName = appUser.FullName,
            DateOfBirth = employeeProfile.DateOfBirth,
            HireDate = employeeProfile.HireDate,
            IsActive = employeeProfile.User.IsActive,
            LeaveDate = employeeProfile.LeaveDate,
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

    // Cập nhật thông tin nhân viên tại chi nhánh ADMIN được phân công
    public async Task<EmployeeUpdateResultDTO> UpdateEmployeeAsync(
        int currentAdminId,
        int employeeId,
        int branchId,
        EmployeeUpdateDTO request)
    {
        DateOnly currentDate = DateOnly.FromDateTime(DateTime.Today);
        // Bước 1: kiểm tra chi nhánh tồn tại và chưa bị xóa
        Branch? branch = await _branchRepository.GetNotDeletedByIdAsync(branchId);
        if (branch == null)
        {
            return new EmployeeUpdateResultDTO

            {

                IsBranchFound = false

            };
        }

        // Bước 2: kiểm tra ADMIN còn được phân công tại chi nhánh
        bool hasAccess = await _branchRepository.HasActiveAssignmentAsync(currentAdminId, branchId, currentDate);
        if (!hasAccess)
        {
            return new EmployeeUpdateResultDTO

            {

                IsBranchFound = true,
                HasAccess = false

            };
        }

        // Cùng khóa với nghỉ/chuyển: chỉ đọc hồ sơ sau khi giành quyền ghi tài khoản.
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            int lockedRows = await _employeeRepository.LockAccountForStatusChangeAsync(employeeId);
            if (lockedRows != 1)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _unitOfWork.ClearTracking();
                return new EmployeeUpdateResultDTO

                {

                    IsBranchFound = true,
                    HasAccess = true

                };
            }

            EmployeeUpdateResultDTO result = await ApplyEmployeeUpdateAsync(currentAdminId, employeeId, branchId, currentDate, request);
            if (result.Employee == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _unitOfWork.ClearTracking();
                return result;
            }

            await _unitOfWork.CommitTransactionAsync();
            return result;
        }
        catch (DbUpdateException exception) when (DatabaseConflict.IsUniqueViolation(exception))
        {
            await _unitOfWork.RollbackTransactionAsync();
            _unitOfWork.ClearTracking();
            EmployeeProfile? employee = await _employeeRepository.GetByIdWithUserAndBranchesAsync(employeeId);
            bool emailDuplicated = employee != null && !string.IsNullOrWhiteSpace(request.Email) && await _userRepository.EmailExistsForOtherUserAsync(request.Email.Trim(), employee.UserId);
            bool codeDuplicated = await _employeeRepository.EmployeeCodeExistsForOtherEmployeeAsync(request.EmployeeCode.Trim(), employeeId);
            if (!emailDuplicated && !codeDuplicated)
            {
                throw;
            }

            return new EmployeeUpdateResultDTO

            {

                IsBranchFound = true,
                HasAccess = true,
                IsEmployeeFound = true,
                IsEmailDuplicated = emailDuplicated,
                IsEmployeeCodeDuplicated = codeDuplicated

            };
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            _unitOfWork.ClearTracking();
            throw;
        }
    }

    private async Task<EmployeeUpdateResultDTO> ApplyEmployeeUpdateAsync(
        int currentAdminId,
        int employeeId,
        int branchId,
        DateOnly currentDate,
        EmployeeUpdateDTO request)
    {
        // Phạm vi được kiểm tra lại trong transaction, không dùng kết quả từ trước khi chờ khóa.
        if (!await _branchRepository.HasActiveAssignmentAsync(currentAdminId, branchId, currentDate))
        {
            return new EmployeeUpdateResultDTO

            {

                IsBranchFound = true,
                HasAccess = false

            };
        }

        // Bước 3: Lấy hồ sơ nhân viên cùng AppUser và lịch sử chi nhánh
        EmployeeProfile? employeeProfile = await _employeeRepository.GetDetailByIdAndBranchAsync(employeeId, branchId, currentDate);
        if (employeeProfile == null)
        {
            return new EmployeeUpdateResultDTO

            {

                IsBranchFound = true,
                HasAccess = true,
                IsEmployeeFound = false

            };
        }

        // Bước 4: chuẩn hóa dữ liệu trước khi kiểm tra và cập nhật
        string fullName = request.FullName.Trim();
        string employeeCode = request.EmployeeCode.Trim();
        string? email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        string? phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        string? position = string.IsNullOrWhiteSpace(request.Position) ? null : request.Position.Trim();
        string? address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        // Bước 5: chỉ kiểm tra Email khi người dùng có nhập dữ liệu
        if (email != null)
        {
            bool emailExists = await _userRepository.EmailExistsForOtherUserAsync(email, employeeProfile.UserId);
            if (emailExists)
            {
                return new EmployeeUpdateResultDTO

                {

                    IsBranchFound = true,
                    HasAccess = true,
                    IsEmployeeFound = true,
                    IsEmailDuplicated = true

                };
            }
        }

        // Bước 6: kiểm tra mã nhân viên có thuộc hồ sơ khác không
        bool employeeCodeExists = await _employeeRepository.EmployeeCodeExistsForOtherEmployeeAsync(employeeCode, employeeProfile.Id);
        if (employeeCodeExists)
        {
            return new EmployeeUpdateResultDTO

            {

                IsBranchFound = true,
                HasAccess = true,
                IsEmployeeFound = true,
                IsEmployeeCodeDuplicated = true

            };
        }

        // Bước 7: cập nhật các trường thuộc bảng AppUser
        employeeProfile.User.FullName = fullName;
        employeeProfile.User.Email = email;
        employeeProfile.User.Phone = phone;
        employeeProfile.User.UpdatedAt = DateTime.UtcNow;
        // Cập nhật các trường thuộc bảng EmployeeProfile
        employeeProfile.EmployeeCode = employeeCode;
        employeeProfile.DateOfBirth = request.DateOfBirth;
        employeeProfile.HireDate = request.HireDate;
        employeeProfile.Position = position;
        employeeProfile.BaseSalary = request.BaseSalary;
        employeeProfile.Address = address;
        employeeProfile.UpdatedAt = DateTime.UtcNow;
        // Entity đang được theo dõi; chỉ lưu các trường đã đổi, không Update toàn bộ graph.
        // Bước 9: lưu các thay đổi xuống db 1 lần
        await _unitOfWork.SaveChangesAsync();
        // Bước 10: chuyển lịch sử phân công chi nhánh thành DTO
        List<EmployeeBranchHistoryDTO> branchHistory = new List<EmployeeBranchHistoryDTO>();
        foreach (UserBranch userBranch in employeeProfile.User.UserBranches)
        {
            // Không trả chi nhánh đã xóa
            if (userBranch.Branch.Deleted)
            {
                continue;
            }

            EmployeeBranchHistoryDTO branchHistoryDTO = new EmployeeBranchHistoryDTO

            {

                BranchId = userBranch.BranchId,
                BranchName = userBranch.Branch.Name,
                ActiveFrom = userBranch.ActiveFrom,
                ActiveTo = userBranch.ActiveTo

            };
            branchHistory.Add(branchHistoryDTO);
        }

        // Bước 11: tạo DTO chứa dữ liệu mới nhất sau khi cập nhật
        EmployeeDetailDTO employeeDTO = new EmployeeDetailDTO

        {

            Id = employeeProfile.Id,
            EmployeeCode = employeeProfile.EmployeeCode,
            FullName = employeeProfile.User.FullName,
            DateOfBirth = employeeProfile.DateOfBirth,
            HireDate = employeeProfile.HireDate,
            LeaveDate = employeeProfile.LeaveDate,
            Position = employeeProfile.Position,
            BaseSalary = employeeProfile.BaseSalary,
            Phone = employeeProfile.User.Phone,
            Email = employeeProfile.User.Email,
            Address = employeeProfile.Address,
            AvatarUrl = employeeProfile.AvatarUrl,
            IsActive = employeeProfile.User.IsActive,
            BranchHistory = branchHistory

        };
        return new EmployeeUpdateResultDTO

        {

            IsBranchFound = true,
            HasAccess = true,
            IsEmployeeFound = true,
            Employee = employeeDTO

        };
    }

    // Endpoint cũ chuyển trực tiếp cũng đi qua workflow mới, không còn đường ghi bỏ qua kiểm tra.
    public async Task<EmployeeChangeBranchResultDTO> ChangeEmployeeBranchAsync(
        int currentAdminId,
        int employeeId,
        int currentBranchId,
        EmployeeChangeBranchDTO request)
    {
        if (request.ActiveFrom != DateOnly.FromDateTime(DateTime.Today))
        {
            return new EmployeeChangeBranchResultDTO

            {

                IsCurrentBranchFound = true,
                IsNewBranchFound = true,
                HasAccess = true,
                IsEmployeeFound = true,
                IsActiveFromValid = false,
                ErrorCode = "Invalid",
                ErrorMessage = "Ngày chuyển phải là hôm nay. Không hỗ trợ hồi tố hoặc đặt lịch chuyển."

            };
        }

        EmployeeTransferResultDTO result = await _employeeTransferService.CreateAsync(
            currentAdminId,
            new EmployeeTransferCreateDTO
            {
                EmployeeId = employeeId,
                SourceBranchId = currentBranchId,
                TargetBranchId = request.NewBranchId,
                TransferImmediately = true
            });
        EmployeeDetailDTO? employee = null;
        if (result.Request?.Status == "TRANSFERRED")
        {
            EmployeeDetailResultDTO detail = await GetEmployeeDetailAsync(currentAdminId, "ADMIN", employeeId, request.NewBranchId);
            employee = detail.Employee;
        }

        return new EmployeeChangeBranchResultDTO

        {

            IsCurrentBranchFound = result.ErrorCode != "NotFound",
            IsNewBranchFound = result.ErrorCode != "Invalid",
            HasAccess = result.ErrorCode != "Forbidden",
            IsEmployeeFound = result.ErrorCode != "NotFound",
            IsSameBranch = result.ErrorCode == "Conflict",
            IsActiveFromValid = true,
            Employee = employee,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage

        };
    }

    // Cập nhật trạng thái nghỉ việc hoặc hoạt động trở lại của nhân viên
    public async Task<EmployeeStatusUpdateResultDTO> UpdateEmployeeStatusAsync(
        int currentAdminId,
        int employeeId,
        int branchId,
        EmployeeStatusUpdateDTO request)
    {
        DateOnly currentDate = DateOnly.FromDateTime(DateTime.Today);
        // Bước 1: kiểm tra chi nhánh tồn tại và chưa bị xóa
        Branch? branch = await _branchRepository.GetNotDeletedByIdAsync(branchId);
        if (branch == null)
        {
            return new EmployeeStatusUpdateResultDTO

            {

                IsBranchFound = false

            };
        }

        // Bước 2: kiểm tra ADMIN có được phân công tại chi nhánh
        bool hasAccess = await _branchRepository.HasActiveAssignmentAsync(currentAdminId, branchId, currentDate);
        if (!hasAccess)
        {
            return new EmployeeStatusUpdateResultDTO

            {

                IsBranchFound = true,
                HasAccess = false

            };
        }

        // Khóa tài khoản trước khi đọc trạng thái để request sau thấy dữ liệu đã commit.
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            int lockedRows = await _employeeRepository.LockAccountForStatusChangeAsync(employeeId);
            if (lockedRows != 1)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new EmployeeStatusUpdateResultDTO

                {

                    IsBranchFound = true,
                    HasAccess = true,
                    IsEmployeeFound = false

                };
            }

            EmployeeStatusUpdateResultDTO result = await ApplyEmployeeStatusAsync(employeeId, branchId, branch, currentDate, request);
            if (result.Employee is null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _unitOfWork.ClearTracking();
                return result;
            }

            await _unitOfWork.CommitTransactionAsync();
            return result;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            _unitOfWork.ClearTracking();
            throw;
        }
    }

    // Chỉ gọi sau khi đã giành khóa tài khoản trong cùng transaction.
    private async Task<EmployeeStatusUpdateResultDTO> ApplyEmployeeStatusAsync(
        int employeeId,
        int branchId,
        Branch branch,
        DateOnly currentDate,
        EmployeeStatusUpdateDTO request)
    {
        // Bước 3: lấy nhân viên cùng tài khoản và lịch sử phân công
        EmployeeProfile? employeeProfile = await _employeeRepository.GetByIdWithUserAndBranchesAsync(employeeId);
        if (employeeProfile == null)
        {
            return new EmployeeStatusUpdateResultDTO

            {

                IsBranchFound = true,
                HasAccess = true,
                IsEmployeeFound = false

            };
        }

        // Tìm phân công chứng minh nhân viên thuộc phạm vi chi nhánh
        UserBranch? employeeBranchAssignment = null;
        // Kiêm tra trạng thái hiện tại từ tài khoản nhân viên
        if (employeeProfile.User.IsActive)
        {
            // Nhân viên đang hoạt động phải có phân công còn hiểu lực
            employeeBranchAssignment = employeeProfile.User.UserBranches.FirstOrDefault(userBranch => userBranch.BranchId == branchId && !userBranch.Branch.Deleted && userBranch.ActiveFrom <= currentDate && (userBranch.ActiveTo == null || userBranch.ActiveTo >= currentDate));
        }
        else if (employeeProfile.LeaveDate.HasValue)
        {
            // Nhân viên đã nghỉ phải được thao tác tại chi nhanh nơi họ nghỉ
            DateOnly lastWorkingDate = employeeProfile.LeaveDate.Value.AddDays(-1);
            employeeBranchAssignment = employeeProfile.User.UserBranches.FirstOrDefault(userBranch => userBranch.BranchId == branchId && !userBranch.Branch.Deleted && userBranch.ActiveTo == lastWorkingDate);
        }

        if (employeeBranchAssignment == null)
        {
            return new EmployeeStatusUpdateResultDTO

            {

                IsBranchFound = true,
                HasAccess = true,
                IsEmployeeFound = false

            };
        }

        // Bước 4: ngày hiệu lực phải là ngày hiện tại
        if (!request.IsActive.HasValue || request.EffectiveDate != currentDate)
        {
            return new EmployeeStatusUpdateResultDTO

            {

                IsBranchFound = true,
                HasAccess = true,
                IsEmployeeFound = true,
                IsEffectiveDateValid = false

            };
        }

        // Controller đã kiểm tra Required nên IsActive có giá trị
        bool newStatus = request.IsActive!.Value;
        UserBranch? newAssignment = null;
        if (newStatus && !branch.IsActive)
        {
            return new EmployeeStatusUpdateResultDTO

            {

                IsBranchFound = true,
                HasAccess = true,
                IsEmployeeFound = true,
                IsEffectiveDateValid = true,
                IsBranchInactive = true

            };
        }

        // Bước 5: không cập nhật khi trạng thái mới giống trạng thái hiện tại
        if (newStatus == employeeProfile.User.IsActive)
        {
            return new EmployeeStatusUpdateResultDTO

            {

                IsBranchFound = true,
                HasAccess = true,
                IsEmployeeFound = true,
                IsEffectiveDateValid = true,
                IsSameStatus = true

            };
        }

        // Bổ sung: ngày nghỉ phải sau ngày bắt đầu phân công hiện tại
        if (!newStatus && request.EffectiveDate <= employeeBranchAssignment.ActiveFrom)
        {
            return new EmployeeStatusUpdateResultDTO

            {

                IsBranchFound = true,
                HasAccess = true,
                IsEmployeeFound = true,
                IsEffectiveDateValid = false,
                IsSameStatus = false

            };
        }

        // Bước 6: không thay đổi trạng thái khi còn phân công tương lai
        bool hasFutureAssignment = employeeProfile.User.UserBranches.Any(userBranch => userBranch.ActiveFrom > currentDate);
        if (hasFutureAssignment)
        {
            return new EmployeeStatusUpdateResultDTO

            {

                IsBranchFound = true,
                HasAccess = true,
                IsEmployeeFound = true,
                IsEffectiveDateValid = true,
                IsSameStatus = false,
                HasFutureAssignment = true

            };
        }

        // Bước 7: xử lý cho nhân viên nghỉ việc
        if (!newStatus)
        {
            employeeProfile.User.IsActive = false;
            employeeProfile.LeaveDate = request.EffectiveDate;
            // Phân công kết thúc trước ngày nghỉ 1 ngày
            employeeBranchAssignment.ActiveTo = request.EffectiveDate.AddDays(-1);
            _userBranchRepository.Update(employeeBranchAssignment);
        }
        // Xử lý cho nhân viên hoạt động trở lại
        else
        {
            employeeProfile.User.IsActive = true;
            employeeProfile.LeaveDate = null;
            // Tạo phân công hồ sơ mới thay vì sửa lịch sử phân công cũ
            newAssignment = new UserBranch

            {

                UserId = employeeProfile.UserId,
                BranchId = branchId,
                ActiveFrom = request.EffectiveDate,
                ActiveTo = null

            };
            await _userBranchRepository.AddAsync(newAssignment);
        }

        // Cập nhật thời gian chỉnh sửa của tài khoản và hồ sơ
        employeeProfile.User.UpdatedAt = DateTime.UtcNow;
        employeeProfile.UpdatedAt = DateTime.UtcNow;
        // Entity đã được đọc có tracking trong transaction; chỉ lưu thuộc tính đã thay đổi.
        // Cả nghỉ và quay lại đều không khôi phục phiên cũ; token đã dùng vẫn giữ UsedAt.
        await _employeeTransferRepository.InvalidatePendingAsync(employeeProfile.Id, "Trạng thái làm việc của nhân viên đã thay đổi.", DateTime.UtcNow);
        await _authSessionRepository.RevokeAllForEmployeeStatusAsync(employeeProfile.UserId, DateTime.UtcNow);
        // Tài khoản, phân công và thu hồi phiên cùng commit hoặc cùng rollback.
        await _unitOfWork.SaveChangesAsync();
        // Bước 9: chuyển lịch sử phân công cũ thành DTO
        List<EmployeeBranchHistoryDTO> branchHistory = new List<EmployeeBranchHistoryDTO>();
        foreach (UserBranch userBranch in employeeProfile.User.UserBranches)
        {
            if (ReferenceEquals(userBranch, newAssignment) || userBranch.Branch.Deleted)
            {
                continue;
            }

            EmployeeBranchHistoryDTO historyDTO = new EmployeeBranchHistoryDTO

            {

                BranchId = userBranch.BranchId,
                BranchName = userBranch.Branch.Name,
                ActiveFrom = userBranch.ActiveFrom,
                ActiveTo = userBranch.ActiveTo

            };
            branchHistory.Add(historyDTO);
        }

        // Thêm phân công mới đúng một lần, kể cả khi EF đã cập nhật collection.
        if (newAssignment != null)
        {
            branchHistory.Add(new EmployeeBranchHistoryDTO
            {
                BranchId = branch.Id,
                BranchName = branch.Name,
                ActiveFrom = newAssignment.ActiveFrom,
                ActiveTo = newAssignment.ActiveTo
            });
        }

        // Bước 10: tạo DTO chứa dữ liệu sau khi cập nhật
        EmployeeDetailDTO employeeDTO = new EmployeeDetailDTO

        {

            Id = employeeProfile.Id,
            EmployeeCode = employeeProfile.EmployeeCode,
            FullName = employeeProfile.User.FullName,
            DateOfBirth = employeeProfile.DateOfBirth,
            HireDate = employeeProfile.HireDate,
            LeaveDate = employeeProfile.LeaveDate,
            Position = employeeProfile.Position,
            BaseSalary = employeeProfile.BaseSalary,
            Phone = employeeProfile.User.Phone,
            Email = employeeProfile.User.Email,
            Address = employeeProfile.Address,
            AvatarUrl = employeeProfile.AvatarUrl,
            IsActive = employeeProfile.User.IsActive,
            BranchHistory = branchHistory

        };
        return new EmployeeStatusUpdateResultDTO

        {

            IsBranchFound = true,
            HasAccess = true,
            IsEmployeeFound = true,
            IsEffectiveDateValid = true,
            IsSameStatus = false,
            HasFutureAssignment = false,
            Employee = employeeDTO

        };
    }
}
