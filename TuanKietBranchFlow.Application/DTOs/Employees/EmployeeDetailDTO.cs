namespace TuanKietBranchFlow.Application.DTOs.Employees;

public class EmployeeDetailDTO
{
    // Id của hồ sơ trong bảng EmployeeProfile
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; } 
    public DateOnly HireDate { get; set; } 
    public string? Position { get; set; }
    public decimal BaseSalary { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? AvatarUrl { get; set; }

    // Lịch sử các chi nhánh nhân viên từng được phân công
    public List<EmployeeBranchHistoryDTO> BranchHistory { get; set; } = new List<EmployeeBranchHistoryDTO>();
    
}