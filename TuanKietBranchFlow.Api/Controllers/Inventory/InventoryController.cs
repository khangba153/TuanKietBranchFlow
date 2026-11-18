using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.Inventory;
using TuanKietBranchFlow.Application.Services.Inventory;

namespace TuanKietBranchFlow.Api.Controllers.Inventory;

[ApiController]
[Route("api/inventory")]
[Authorize(Roles = "ADMIN,OWNER,EMPLOYEE")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync([FromQuery] InventoryQueryDTO query)
    {
        string role = User.IsInRole("OWNER") ? "OWNER" : User.IsInRole("ADMIN") ? "ADMIN" : "EMPLOYEE";
        InventoryResultDTO result = await _inventoryService.GetStockAsync(UserId, role, query);
        if (result.ErrorCode != null)
        {
            return ToResponse(result, false);
        }

        return Ok(result.Inventory);
    }

    [HttpPost("configurations")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ConfigureAsync(InventoryConfigurationRequestDTO request)
    {
        return ToResponse(await _inventoryService.ConfigureAsync(UserId, request), true);
    }

    [HttpPut("configurations/{id:int:min(1)}/warning-threshold")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ThresholdAsync(
        int id,
        InventoryThresholdRequestDTO request)
    {
        return ToResponse(await _inventoryService.UpdateThresholdAsync(UserId, id, request), false);
    }

    private int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId) ? userId : 0;

    private IActionResult ToResponse(
        InventoryResultDTO result,
        bool created)
    {
        if (result.ErrorCode != null)
        {
            int status = result.ErrorCode switch
            {
                "Forbidden" => 403,
                "NotFound" => 404,
                "Conflict" => 409,
                _ => 400
            };
            return Problem(statusCode: status, detail: result.ErrorMessage);
        }

        if (created)
        {
            return StatusCode(201, result);
        }

        return Ok(result);
    }
}
