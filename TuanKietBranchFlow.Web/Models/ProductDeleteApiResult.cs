namespace TuanKietBranchFlow.Web.Models;

public class ProductDeleteApiResult
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}