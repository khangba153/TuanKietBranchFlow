using TuanKietBranchFlow.Application.Services.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.Roles;

namespace TuanKietBranchFlow.Api.Controllers.Roles;

[ApiController]
[Route("api/roles")]
[Authorize(Roles = "OWNER,ADMIN")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;
    // Nhận RoleService từ DI
    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    /// <summary>
    /// Lấy danh sách Role chưa bị xóa.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<RoleDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<RoleDTO>>> GetAllRolesAsync()
    {
        // Gọi service lấy danh sách Role đã chuyển thành DTO
        List<RoleDTO> roles = await _roleService.GetAllRolesAsync();
        return Ok(roles);
    }
}
