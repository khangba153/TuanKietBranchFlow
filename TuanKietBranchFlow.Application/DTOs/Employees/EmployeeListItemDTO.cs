namespace TuanKietBranchFlow.Application.DTOs.Employees;

public class EmployeeListItemDTO
{
    // Id của hồ sơ nhân viên trong bảng EmployeeProfile
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string? Position { get; set; }

    // Chi nhánh hiện tại được chọn trên giao diện
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateOnly HireDate { get; set; }
    public bool IsActive { get; set; }
    public string? AvatarUrl { get; set; }
}