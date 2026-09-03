namespace TuanKietBranchFlow.Application.DTOs.Employees;

public class EmployeeChangeBranchResultDTO
{
    // Cho biết chi nhánh hiện tại có tồn tại hay không
    public bool IsCurrentBranchFound { get; set; }

    // Cho biết chi nhánh mới có tồn tại hay không
    public bool IsNewBranchFound { get; set; }

    // Cho biết ADMIN có quyền tại cả 2 chi nhánh hay không
    public bool HasAccess { get; set; }

    // Cho biết nhân viên và phân công hiện tại có tồn tại hay không
    public bool IsEmployeeFound { get; set; }

    // Cho biết chi nhánh mới có trùng với chi nhánh hiện tại hay không.
    public bool IsSameBranch { get; set; }

    // Cho biết ngày bắt đầu phải lớn hơn ngày phân công hiện tại
    public bool IsActiveFromValid { get; set; }

    // Chỉ có dữ liệu khi chuyển chi nhánh thành công
    public EmployeeDetailDTO? Employee { get; set; }
}