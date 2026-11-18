using TuanKietBranchFlow.Application.DTOs.MenuManagement.Sizes;

namespace TuanKietBranchFlow.Web.Models.MenuManagement.Sizes;

public class SizeCreateApiResult
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public MenuSizeDTO? Size { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
