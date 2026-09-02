namespace TuanKietBranchFlow.Application.DTOs.Employees;

public class EmployeeCreateResultDTO
{
    // Cho biết chi nhánh tồn tại và chưa bị xóa
    public bool IsBranchFound { get; set; }
    
    // Cho biết ADMIN còn được phân công tại chi nhánh
    public bool HasAccess { get; set; }

    // Cho biết db có Role EMPLOYEE để gán cho tài khoản mới
    public bool IsEmployeeRoleFound { get; set; }

    // Các cờ kiểm tra dữ liệu không được phép trùng
    public bool IsUsernameDuplicated { get; set; }
    public bool IsEmailDuplicated { get; set; }
    public bool IsEmployeeCodeDuplicated { get; set; }

    // Chứa nhân viên vừa tạo khi tất cả điều kiện hợp lệ
    public EmployeeDetailDTO? Employee { get; set; }
}