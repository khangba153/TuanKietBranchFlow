namespace TuanKietBranchFlow.Application.DTOs.MenuManagement.Products;

public class CreateProductResultDTO
{
    public bool IsRequestInvalid { get; set; }
    public bool IsCategoryNotFound { get; set; }
    public bool IsNameDuplicated { get; set; }
    public bool IsProductSizesInvalid { get; set; }
    public MenuProductDTO? Product { get; set; }
}