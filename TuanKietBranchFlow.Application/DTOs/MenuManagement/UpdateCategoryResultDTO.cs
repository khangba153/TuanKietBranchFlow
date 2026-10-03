namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class UpdateCategoryResultDTO
{
    public bool IsCategoryNotFound { get; set; }
    public bool IsRequestInvalid { get; set; }
    public bool IsNameDuplicated { get; set; }
    public MenuCategoryDTO? Category { get; set; }
}