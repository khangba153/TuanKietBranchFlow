using TuanKietBranchFlow.Application.DTOs.MenuManagement.Categories;

namespace TuanKietBranchFlow.Web.Models.MenuManagement.Categories;

public class CategoryUpdateApiResult
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public MenuCategoryDTO? Category { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}