using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.Employees.Transfers;

public class EmployeeTransferDecisionDTO
{
    [StringLength(500)]
    public string? Reason { get; set; }
}
