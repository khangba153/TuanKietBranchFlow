using TuanKietBranchFlow.Application.DTOs.MenuManagement.Products;

namespace TuanKietBranchFlow.Web.Models.MenuManagement.Products;

public class ProductCreateApiResult
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public MenuProductDTO? Product { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}