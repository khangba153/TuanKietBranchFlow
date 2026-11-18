namespace TuanKietBranchFlow.Application.DTOs.MenuManagement.Categories;

public class CreateCategoryResultDTO
{
    public bool IsNameInvalid { get; set; }
    public bool IsNameDuplicated { get; set; }
    public MenuCategoryDTO? Category { get; set; }
}
