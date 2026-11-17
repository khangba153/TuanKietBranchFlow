namespace TuanKietBranchFlow.Infrastructure.Models;

// Scaffold từ bảng EmployeeTransferRequest, script 006.
public partial class EmployeeTransferRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int SourceAssignmentId { get; set; }
    public int SourceBranchId { get; set; }
    public int TargetBranchId { get; set; }
    public int RequestedByUserId { get; set; }
    public int? ProcessedByUserId { get; set; }
    public string EmployeeCode { get; set; } = null!;
    public string EmployeeName { get; set; } = null!;
    public string SourceBranchName { get; set; } = null!;
    public string TargetBranchName { get; set; } = null!;
    public string RequestedByName { get; set; } = null!;
    public string? ProcessedByName { get; set; }
    public string Status { get; set; } = null!;
    public string? Note { get; set; }
    public string? ResolutionReason { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateOnly? EffectiveDate { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}
