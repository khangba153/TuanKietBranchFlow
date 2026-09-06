namespace TuanKietBranchFlow.Application.DTOs.Employees;

public class EmployeeStatusUpdateResultDTO
{
    // Cho biết chi nhánh có tồn tại hay không.
    public bool IsBranchFound { get; set; }

    // Cho biết ADMIN có quyền thao tác tại chi nhánh hay không.
    public bool HasAccess { get; set; }

    // Cho biết nhân viên có tồn tại hay không.
    public bool IsEmployeeFound { get; set; }

    // Cho biết trạng thái mới có trùng với trạng thái hiện tại không.
    public bool IsSameStatus { get; set; }

    // Cho biết nhân viên có lịch phân công trong tương lai hay không
    public bool HasFutureAssignment { get; set; }

    // Cho biết ngày hiệu lực có hợp lệ hay không.
    public bool IsEffectiveDateValid { get; set; }

    // Chứa thông tin nhân viên sau khi cập nhật thành công.
    public EmployeeDetailDTO? Employee { get; set; }
}