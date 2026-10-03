namespace TuanKietBranchFlow.Web.Models;

public class CategoryDeleteApiResult
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}