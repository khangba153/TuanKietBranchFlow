using TuanKietBranchFlow.Application.DTOs.MenuManagement;

namespace TuanKietBranchFlow.Web.Models;

public class ProductCreateApiResult
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public MenuProductDTO? Product { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}