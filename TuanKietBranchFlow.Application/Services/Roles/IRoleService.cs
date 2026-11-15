using TuanKietBranchFlow.Application.DTOs.Roles;

namespace TuanKietBranchFlow.Application.Services.Roles;

public interface IRoleService
{
    // Lấy danh sách role dùng cho giao diện quản lý tài khoản
    Task<List<RoleDTO>> GetAllRolesAsync();
}