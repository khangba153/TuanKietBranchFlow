using TuanKietBranchFlow.Application.DTOs.Branches;

namespace TuanKietBranchFlow.Application.Services;

public interface IBranchService
{
    // Lấy các chi nhánh hiện tại người dùng được phép truy cập
    Task<List<AccessibleBranchDTO>> GetAccessibleBranchesAsync(int userId, string role);

    // Lấy 1 chi nhánh nếu người dùng có quyền truy cập
    Task<BranchAccessResultDTO> GetAccessibleBranchByIdAsync(int userId, string role, int branchId);
}