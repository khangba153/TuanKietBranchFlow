using TuanKietBranchFlow.Application.DTOs.MenuManagement;

namespace TuanKietBranchFlow.Web.Models;

public class CategoryUpdateApiResult
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public MenuCategoryDTO? Category { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}