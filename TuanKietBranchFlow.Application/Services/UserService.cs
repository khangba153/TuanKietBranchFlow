using TuanKietBranchFlow.Application.DTOs.Users;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories;

namespace TuanKietBranchFlow.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    // Nhận UserRepository từ DI
    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    // Lấy và chuyển dữ liệu người dùng thành DTO
    public async Task<UserProfileDTO?> GetCurrentProfileAsync(int userId)
    {
        AppUser? user = await _userRepository.GetProfileByIdAsync(userId);

        // Không trả hồ sơ nếu tài khoản hoặc Role không hợp lệ
        if (user == null || user.Role.Deleted)
        {
            return null;
        }

        EmployeeProfile? employeeProfile = user.EmployeeProfile;
        // Hồ sơ nhân viên đã xóa mềm được xem như không có hồ sơ
        if (employeeProfile?.Deleted == true)
        {
            employeeProfile = null;
        }

        UserBranch? currentUserBranch = null;

        // EMPLOYEE chỉ làm tại 1 chi nhánh hiện tại
        if (user.Role.Code == "EMPLOYEE")
        {
            DateOnly currentDate = DateOnly.FromDateTime(DateTime.Today);

            currentUserBranch = user.UserBranches
                .Where(userBranch => 
                    userBranch.ActiveFrom <= currentDate
                    && (userBranch.ActiveTo == null || userBranch.ActiveTo >= currentDate)
                    && userBranch.Branch.IsActive
                    && !userBranch.Branch.Deleted)
                .FirstOrDefault();
        }
        
        // Những trường giao diện cần dùng
        return new UserProfileDTO
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role.Code,

            EmployeeCode = employeeProfile?.EmployeeCode,
            HireDate = employeeProfile?.HireDate,
            Position = employeeProfile?.Position,
            Address = employeeProfile?.Address,
            AvatarUrl = employeeProfile?.AvatarUrl,

            CurrentBranchId = currentUserBranch?.BranchId,
            CurrentBranchName = currentUserBranch?.Branch.Name
        };
    }
}