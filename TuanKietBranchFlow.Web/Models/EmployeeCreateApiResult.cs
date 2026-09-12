using TuanKietBranchFlow.Application.DTOs.Employees;

namespace TuanKietBranchFlow.Web.Models;

public class EmployeeCreateApiResult
{
    // Cho biết request tạo nhân viên có thành công hay không
    public bool IsSuccess { get; set; }

    // Lưu HTTP status code
    public int StatusCode { get; set; }

    // Chứa nhân viên vừa tạo khi API trả thành công
    public EmployeeDetailDTO? Employee { get; set; }

    // Chứa thông báo lỗi đọc từ ProblemDetails
    public string ErrorMessage { get; set; } = string.Empty;
}