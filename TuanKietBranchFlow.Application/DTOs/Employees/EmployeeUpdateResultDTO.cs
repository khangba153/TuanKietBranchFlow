namespace TuanKietBranchFlow.Application.DTOs.Employees;

public class EmployeeUpdateResultDTO
{
    // Cho biết chi nhánh còn tồn tại hay đã xóa
    public bool IsBranchFound { get; set; }
    
    // Cho biết ADMIN có quyền thao tác tại chi nhánh hay không
    public bool HasAccess { get; set; }

    // Cho biết nhân viên có tồn tại tại chi nhánh hay không
    public bool IsEmployeeFound { get; set; }

    // Cho biết email có được tài khoản khác sử dụng hay không
    public bool IsEmailDuplicated { get; set; }

    // Cho biết mã nhân viên có được hồ sơ khác sử dụng hay không
    public bool IsEmployeeCodeDuplicated { get; set; }

    // Chỉ có dữ liệu khi cập nhật nhân viên thành công
    public EmployeeDetailDTO? Employee { get; set; }
}