namespace TuanKietBranchFlow.Application.DTOs.Employees;

public class EmployeeBranchHistoryDTO
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateOnly ActiveFrom { get; set; }
    public DateOnly? ActiveTo { get; set; }
}