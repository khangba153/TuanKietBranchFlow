using TuanKietBranchFlow.Application.DTOs.Employees;

namespace TuanKietBranchFlow.Web.Models.Employees;

public class EmployeeDetailApiResult
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public EmployeeDetailDTO? Employee { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public IDictionary<string, string[]> ValidationErrors { get; set; } = new Dictionary<string, string[]>();
}
