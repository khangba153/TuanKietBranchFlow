using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.Employees.Transfers;

public class EmployeeTransferCreateDTO
{
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }
    [Range(1, int.MaxValue)]
    public int SourceBranchId { get; set; }
    [Range(1, int.MaxValue)]
    public int TargetBranchId { get; set; }
    public bool TransferImmediately { get; set; }
    [StringLength(500)]
    public string? Note { get; set; }
}
