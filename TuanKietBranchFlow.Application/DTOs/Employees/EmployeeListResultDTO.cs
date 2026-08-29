namespace TuanKietBranchFlow.Application.DTOs.Employees;

public class EmployeeListResultDTO
{
    // Cho biết chi nhánh còn tồn tại hay đã xóa
    public bool IsBranchFound { get; set; }

    // Cho biết người dùng có quyền truy cập chi nhánh hay không
    public bool HasAccess { get; set; }

    // Danh sách nhân viên được trả khi có quyền truy cập
    public List<EmployeeListItemDTO> Employees { get; set; } = new List<EmployeeListItemDTO>();
}