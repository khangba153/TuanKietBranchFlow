namespace TuanKietBranchFlow.Application.DTOs.Employees.Transfers;

public class EmployeeTransferDTO
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int SourceBranchId { get; set; }
    public int TargetBranchId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string SourceBranchName { get; set; } = string.Empty;
    public string TargetBranchName { get; set; } = string.Empty;
    public string RequestedByName { get; set; } = string.Empty;
    public string? ProcessedByName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? ResolutionReason { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateOnly? EffectiveDate { get; set; }
}
