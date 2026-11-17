namespace TuanKietBranchFlow.Application.DTOs.Employees.Transfers;

public class EmployeeTransferTargetDTO
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool CanTransferImmediately { get; set; }
}
