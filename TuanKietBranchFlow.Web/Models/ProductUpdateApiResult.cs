using TuanKietBranchFlow.Application.DTOs.MenuManagement;

namespace TuanKietBranchFlow.Web.Models;

public class ProductUpdateApiResult
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public MenuProductDTO? Product { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}