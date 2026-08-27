using TuanKietBranchFlow.Application.DTOs.Roles;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories;

namespace TuanKietBranchFlow.Application.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;

    // Nhận RoleRepository từ DI
    public RoleService(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    // Lấy danh sách Role và chuyển thành DTO
    public async Task<List<RoleDTO>> GetAllRolesAsync()
    {
        // Repository chịu trách nhiệm truy vấn db
        List<Role> roles = await _roleRepository.GetAllNotDeletedAsync();

        // Service chọn trường trả cho giao diện
        List<RoleDTO> result = roles
            .Select(role => new RoleDTO
            {
                Id = role.Id,
                Code = role.Code,
                Name = role.Name
            }).ToList();

        return result;
    }
}