namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class UpdateProductResultDTO
{
    public bool IsProductNotFound { get; set; }
    public bool IsCategoryNotFound { get; set; }
    public bool IsRequestInvalid { get; set; }
    public bool IsProductSizesInvalid { get; set; }
    public bool IsNameDuplicated { get; set; }

    public MenuProductDTO? Product { get; set; }
}