namespace TuanKietBranchFlow.Application.DTOs.Employees.Transfers;

public class EmployeeTransferResultDTO
{
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public EmployeeTransferDTO? Request { get; set; }
}
