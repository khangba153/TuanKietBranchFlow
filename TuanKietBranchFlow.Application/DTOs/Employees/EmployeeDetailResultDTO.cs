namespace TuanKietBranchFlow.Application.DTOs.Employees;

public class EmployeeDetailResultDTO
{
    // Cho biết chi nhánh còn tồn tại hay đã xóa
    public bool IsBranchFound { get; set; }

    // Cho biết OWNER hoặc ADMIN có quyền truy cập chi nhánh hay không
    public bool HasAccess { get; set; }

    // Cho biết nhân viên có tồn tại và đang thuộc chi nhánh hay không
    public bool IsEmployeeFound { get; set; }

    // Chứa dữ liệu chi tiết khi tất cả điều kiện hợp lệ
    public EmployeeDetailDTO? Employee { get; set; }
}