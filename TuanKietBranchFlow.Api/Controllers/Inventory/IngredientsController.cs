using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.Inventory;
using TuanKietBranchFlow.Application.Services.Inventory;

namespace TuanKietBranchFlow.Api.Controllers.Inventory;

[ApiController]
[Route("api/ingredients")]
[Authorize(Roles = "ADMIN,OWNER")]
public class IngredientsController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    public IngredientsController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync([FromQuery] string? search)
    {
        if (search?.Length > 150)
        {
            return Problem(statusCode: 400, detail: "Từ khóa tối đa 150 ký tự.");
        }

        return Ok(await _inventoryService.GetIngredientsAsync(search));
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> CreateAsync(InventoryCatalogRequestDTO request)
    {
        InventoryResultDTO result = await _inventoryService.CreateIngredientAsync(
            request.Name,
            request.UnitId);

        return ToResponse(result, true);
    }

    [HttpPut("{id:int:min(1)}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateAsync(
        int id,
        InventoryCatalogRequestDTO request)
    {
        InventoryResultDTO result = await _inventoryService.UpdateIngredientAsync(
            id,
            request.Name,
            request.UnitId);

        return ToResponse(result, false);
    }

    [HttpPatch("{id:int:min(1)}/status")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> StatusAsync(
        int id,
        InventoryStatusRequestDTO request)
    {
        InventoryResultDTO result = await _inventoryService.UpdateIngredientStatusAsync(
            id,
            request.IsActive!.Value);

        return ToResponse(result, false);
    }

    private IActionResult ToResponse(
        InventoryResultDTO result,
        bool created)
    {
        if (result.ErrorCode != null)
        {
            int status = StatusCodes.Status400BadRequest;

            if (result.ErrorCode == "NotFound")
            {
                status = StatusCodes.Status404NotFound;
            }
            else if (result.ErrorCode == "Conflict")
            {
                status = StatusCodes.Status409Conflict;
            }
            return Problem(statusCode: status, detail: result.ErrorMessage);
        }

        if (created)
        {
            return StatusCode(StatusCodes.Status201Created, result);
        }

        return Ok(result);
    }
}
