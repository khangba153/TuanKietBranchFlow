using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Application.DTOs.Employees.Transfers;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories.Auth;
using TuanKietBranchFlow.Infrastructure.Repositories.Branches;
using TuanKietBranchFlow.Infrastructure.Repositories.Employees;
using TuanKietBranchFlow.Infrastructure.Repositories.Users;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;

namespace TuanKietBranchFlow.Application.Services.Employees;

public class EmployeeTransferService : IEmployeeTransferService
{
    private readonly IEmployeeTransferRepository _employeeTransferRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IUserBranchRepository _userBranchRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly IUnitOfWork _unitOfWork;
    public EmployeeTransferService(
        IEmployeeTransferRepository transferRepository,
        IEmployeeRepository employeeRepository,
        IBranchRepository branchRepository,
        IUserBranchRepository assignmentRepository,
        IUserRepository userRepository,
        IAuthSessionRepository sessionRepository,
        IUnitOfWork unitOfWork)
    {
        _employeeTransferRepository = transferRepository;
        _employeeRepository = employeeRepository;
        _branchRepository = branchRepository;
        _userBranchRepository = assignmentRepository;
        _userRepository = userRepository;
        _authSessionRepository = sessionRepository;
        _unitOfWork = unitOfWork;
    }

    // Chỉ trả mã/tên đích, không mở quyền đọc nhân sự hoặc doanh số ngoài phạm vi.
    public async Task<List<EmployeeTransferTargetDTO>?> GetTargetsAsync(
        int adminId,
        int sourceBranchId)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        if (!await HasAdminAccessAsync(adminId, sourceBranchId, today))
        {
            return null;
        }

        List<Branch> branches = await _branchRepository.GetActiveNotDeletedAsync();
        List<EmployeeTransferTargetDTO> result = new List<EmployeeTransferTargetDTO>();
        foreach (Branch branch in branches)
        {
            if (branch.Id == sourceBranchId)
            {
                continue;
            }

            result.Add(new EmployeeTransferTargetDTO
            {
                Id = branch.Id,
                Code = branch.Code,
                Name = branch.Name,
                CanTransferImmediately = await _branchRepository.HasActiveAssignmentAsync(adminId, branch.Id, today)
            });
        }

        return result;
    }

    public async Task<List<EmployeeTransferDTO>> GetRequestsAsync(
        int userId,
        bool isOwner,
        bool incoming,
        int page)
    {
        List<EmployeeTransferRequest> requests = await _employeeTransferRepository.GetVisibleAsync(userId, isOwner, incoming, page, DateOnly.FromDateTime(DateTime.Today));
        return requests.Select(Map).ToList();
    }

    // Một transaction bảo vệ kiểm tra, phân công, trạng thái yêu cầu và thu hồi phiên.
    public async Task<EmployeeTransferResultDTO> CreateAsync(
        int adminId,
        EmployeeTransferCreateDTO input)
    {
        if (input.EmployeeId <= 0 || input.SourceBranchId <= 0 || input.TargetBranchId <= 0 || (input.Note?.Trim().Length ?? 0) > 500)
        {
            return Error("Invalid", "Dữ liệu yêu cầu không hợp lệ.");
        }

        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        if (!await HasAdminAccessAsync(adminId, input.SourceBranchId, today))
        {
            return Error("Forbidden", "Bạn không quản lý chi nhánh nguồn.");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            if (await _employeeRepository.LockAccountForStatusChangeAsync(input.EmployeeId) != 1)
            {
                return await RejectTransactionAsync("NotFound", "Không tìm thấy nhân viên.");
            }

            EmployeeProfile? employee = await _employeeRepository.GetByIdWithUserAndBranchesAsync(input.EmployeeId);
            UserBranch? sourceAssignment = FindSourceAssignment(employee, input.SourceBranchId, today);
            if (sourceAssignment == null)
            {
                return await RejectTransactionAsync("NotFound", "Nhân viên không còn làm việc tại chi nhánh nguồn.");
            }

            string? invalidReason = ValidateAssignments(employee!, sourceAssignment, today);
            if (invalidReason != null)
            {
                return await RejectTransactionAsync("Conflict", invalidReason);
            }

            Branch? target = await _branchRepository.GetNotDeletedByIdAsync(input.TargetBranchId);
            Branch? source = await _branchRepository.GetNotDeletedByIdAsync(input.SourceBranchId);
            if (target == null || !target.IsActive || source == null || target.Id == source.Id)
            {
                return await RejectTransactionAsync("Invalid", "Chi nhánh đích phải đang hoạt động và khác chi nhánh nguồn.");
            }

            if (!await HasAdminAccessAsync(adminId, source.Id, today))
            {
                return await RejectTransactionAsync("Forbidden", "Quyền quản lý nguồn đã thay đổi. Hãy tải lại.");
            }

            bool canTransferImmediately = await HasAdminAccessAsync(adminId, target.Id, today);
            if (canTransferImmediately != input.TransferImmediately)
            {
                return await RejectTransactionAsync("Conflict", "Phạm vi quản lý đã thay đổi. Hãy tải lại để chọn đúng hình thức chuyển.");
            }

            if (await _employeeTransferRepository.GetPendingAsync(input.EmployeeId) != null)
            {
                return await RejectTransactionAsync(
                    "Conflict",
                    "Nhân viên đã có yêu cầu chờ tiếp nhận. Hãy xử lý hoặc rút yêu cầu đó trước.");
            }

            AppUser? requester = await _userRepository.GetProfileByIdAsync(adminId);
            EmployeeTransferRequest request = new EmployeeTransferRequest

            {

                EmployeeId = employee!.Id,
                SourceAssignmentId = sourceAssignment.Id,
                SourceBranchId = source.Id,
                TargetBranchId = target.Id,
                RequestedByUserId = adminId,
                EmployeeCode = employee.EmployeeCode,
                EmployeeName = employee.User.FullName,
                SourceBranchName = source.Name,
                TargetBranchName = target.Name,
                RequestedByName = requester!.FullName,
                Status = "PENDING",
                Note = input.Note?.Trim(),
                RequestedAt = DateTime.UtcNow

            };
            if (canTransferImmediately)
            {
                await ApplyTransferAsync(employee, sourceAssignment, request, adminId, requester.FullName, today);
            }

            await _employeeTransferRepository.AddAsync(request);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            return new EmployeeTransferResultDTO

            {

                Request = Map(request)

            };
        }
        catch (DbUpdateConcurrencyException)
        {
            return await RejectTransactionAsync("Conflict", "Dữ liệu đã thay đổi. Hãy tải lại yêu cầu.");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            _unitOfWork.ClearTracking();
            throw;
        }
    }

    // Tiếp nhận/từ chối/hủy đều khóa cùng nhân viên rồi mới đọc trạng thái yêu cầu.
    public async Task<EmployeeTransferResultDTO> DecideAsync(
        int adminId,
        int requestId,
        string action,
        string? reason)
    {
        reason = reason?.Trim();
        if (action != "accept" && action != "reject" && action != "cancel")
        {
            return Error("Invalid", "Thao tác không hợp lệ.");
        }

        if ((reason?.Length ?? 0) > 500 || (action == "reject" && string.IsNullOrWhiteSpace(reason)))
        {
            return Error("Invalid", "Từ chối cần có lý do; nội dung tối đa 500 ký tự.");
        }

        int? employeeId = await _employeeTransferRepository.GetEmployeeIdAsync(requestId);
        if (!employeeId.HasValue)
        {
            return Error("NotFound", "Không tìm thấy yêu cầu.");
        }

        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            if (await _employeeRepository.LockAccountForStatusChangeAsync(employeeId.Value) != 1)
            {
                return await RejectTransactionAsync("NotFound", "Không tìm thấy nhân viên.");
            }

            EmployeeTransferRequest? request = await _employeeTransferRepository.GetByIdAsync(requestId);
            if (request == null)
            {
                return await RejectTransactionAsync("NotFound", "Không tìm thấy yêu cầu.");
            }

            int requiredBranchId = action == "cancel" ? request.SourceBranchId : request.TargetBranchId;
            if (!await HasAdminAccessAsync(adminId, requiredBranchId, today))
            {
                return await RejectTransactionAsync("Forbidden", "Bạn không có quyền xử lý yêu cầu tại chi nhánh này.");
            }

            if (request.Status != "PENDING")
            {
                return await RejectTransactionAsync("Conflict", "Yêu cầu đã được xử lý. Hãy tải lại danh sách.");
            }

            AppUser? processor = await _userRepository.GetProfileByIdAsync(adminId);
            EmployeeProfile? employee = await _employeeRepository.GetByIdWithUserAndBranchesAsync(employeeId.Value);
            UserBranch? sourceAssignment = FindSourceAssignment(employee, request.SourceBranchId, today);
            Branch? target = await _branchRepository.GetNotDeletedByIdAsync(request.TargetBranchId);
            bool requesterHasAccess = await HasAdminAccessAsync(request.RequestedByUserId, request.SourceBranchId, today);
            string? invalidReason = sourceAssignment == null || sourceAssignment.Id != request.SourceAssignmentId ? "Phân công nguồn đã thay đổi hoặc nhân viên đã nghỉ việc." : ValidateAssignments(employee!, sourceAssignment, today);
            if (target == null || !target.IsActive || !requesterHasAccess)
            {
                invalidReason = "Chi nhánh đích hoặc quyền quản lý của người gửi không còn phù hợp.";
            }

            if (invalidReason != null && action != "cancel")
            {
                CompleteRequest(request, "INVALIDATED", adminId, processor!.FullName, invalidReason);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return new EmployeeTransferResultDTO

                {

                    Request = Map(request)

                };
            }

            if (action == "accept")
            {
                await ApplyTransferAsync(employee!, sourceAssignment!, request, adminId, processor!.FullName, today);
                request.ResolutionReason = reason;
            }
            else
            {
                CompleteRequest(
                    request,
                    action == "reject" ? "REJECTED" : "CANCELLED",
                    adminId,
                    processor!.FullName,
                    reason);
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            return new EmployeeTransferResultDTO

            {

                Request = Map(request)

            };
        }
        catch (DbUpdateConcurrencyException)
        {
            return await RejectTransactionAsync("Conflict", "Yêu cầu đã thay đổi. Hãy tải lại danh sách.");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            _unitOfWork.ClearTracking();
            throw;
        }
    }

    private async Task<bool> HasAdminAccessAsync(
        int userId,
        int branchId,
        DateOnly today)
    {
        AppUser? user = await _userRepository.GetProfileByIdAsync(userId);
        Branch? branch = await _branchRepository.GetNotDeletedByIdAsync(branchId);
        return user != null && user.IsActive && !user.Deleted && user.Role != null && !user.Role.Deleted && user.Role.Code == "ADMIN" && branch != null && await _branchRepository.HasActiveAssignmentAsync(userId, branchId, today);
    }

    private static UserBranch? FindSourceAssignment(
        EmployeeProfile? employee,
        int branchId,
        DateOnly today)
    {
        if (employee == null || employee.Deleted || employee.User.Deleted || !employee.User.IsActive)
        {
            return null;
        }

        return employee.User.UserBranches.FirstOrDefault(assignment => assignment.BranchId == branchId && !assignment.Branch.Deleted && assignment.ActiveFrom <= today && (!assignment.ActiveTo.HasValue || assignment.ActiveTo >= today));
    }

    // Một nhân viên chỉ có một khoảng phân công hiện tại, không chồng lấn hoặc lịch tương lai.
    public static string? ValidateAssignments(
        EmployeeProfile employee,
        UserBranch source,
        DateOnly today)
    {
        if (source.ActiveFrom >= today)
        {
            return "Chưa thể chuyển trong ngày bắt đầu phân công. Ngày chuyển phải sau ngày bắt đầu phân công nguồn.";
        }

        List<UserBranch> assignments = employee.User.UserBranches.OrderBy(assignment => assignment.ActiveFrom).ToList();
        for (int index = 0; index < assignments.Count; index++)
        {
            UserBranch assignment = assignments[index];
            if (assignment.ActiveFrom > today)
            {
                return "Nhân viên còn lịch phân công tương lai. Cần xử lý lịch trước khi chuyển.";
            }

            if (index > 0 && (!assignments[index - 1].ActiveTo.HasValue || assignments[index - 1].ActiveTo >= assignment.ActiveFrom))
            {
                return "Lịch sử phân công đang chồng lấn. Cần kiểm tra dữ liệu trước khi chuyển.";
            }
        }

        return null;
    }

    private async Task ApplyTransferAsync(
        EmployeeProfile employee,
        UserBranch source,
        EmployeeTransferRequest request,
        int adminId,
        string adminName,
        DateOnly today)
    {
        source.ActiveTo = today.AddDays(-1);
        _userBranchRepository.Update(source);
        await _userBranchRepository.AddAsync(new UserBranch
        {
            UserId = employee.UserId,
            BranchId = request.TargetBranchId,
            ActiveFrom = today,
            CreatedAt = DateTime.UtcNow
        });
        employee.User.UpdatedAt = DateTime.UtcNow;
        employee.UpdatedAt = DateTime.UtcNow;
        await _authSessionRepository.RevokeAllForEmployeeStatusAsync(employee.UserId, DateTime.UtcNow);
        CompleteRequest(request, "TRANSFERRED", adminId, adminName, null);
        request.EffectiveDate = today;
    }

    private static void CompleteRequest(
        EmployeeTransferRequest request,
        string status,
        int adminId,
        string adminName,
        string? reason)
    {
        request.Status = status;
        request.ProcessedByUserId = adminId;
        request.ProcessedByName = adminName;
        request.ProcessedAt = DateTime.UtcNow;
        request.ResolutionReason = reason;
    }

    private async Task<EmployeeTransferResultDTO> RejectTransactionAsync(
        string code,
        string message)
    {
        await _unitOfWork.RollbackTransactionAsync();
        _unitOfWork.ClearTracking();
        return Error(code, message);
    }

    private static EmployeeTransferResultDTO Error(
        string code,
        string message)
    {
        return new EmployeeTransferResultDTO

        {

            ErrorCode = code,
            ErrorMessage = message

        };
    }

    private static EmployeeTransferDTO Map(EmployeeTransferRequest request)
    {
        return new EmployeeTransferDTO

        {

            Id = request.Id,
            EmployeeId = request.EmployeeId,
            SourceBranchId = request.SourceBranchId,
            TargetBranchId = request.TargetBranchId,
            EmployeeCode = request.EmployeeCode,
            EmployeeName = request.EmployeeName,
            SourceBranchName = request.SourceBranchName,
            TargetBranchName = request.TargetBranchName,
            RequestedByName = request.RequestedByName,
            ProcessedByName = request.ProcessedByName,
            Status = request.Status,
            Note = request.Note,
            ResolutionReason = request.ResolutionReason,
            RequestedAt = request.RequestedAt,
            ProcessedAt = request.ProcessedAt,
            EffectiveDate = request.EffectiveDate

        };
    }
}
