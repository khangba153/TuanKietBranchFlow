using TuanKietBranchFlow.Application.DTOs.MenuManagement;

namespace TuanKietBranchFlow.Web.Models;

public class SizeCreateApiResult
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public MenuSizeDTO? Size { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}