using TuanKietBranchFlow.Application.DTOs.Employees.Transfers;

namespace TuanKietBranchFlow.Application.Services.Employees;

public interface IEmployeeTransferService
{
    Task<List<EmployeeTransferTargetDTO>?> GetTargetsAsync(int adminId, int sourceBranchId);
    Task<List<EmployeeTransferDTO>> GetRequestsAsync(int userId, bool isOwner, bool incoming, int page);
    Task<EmployeeTransferResultDTO> CreateAsync(int adminId, EmployeeTransferCreateDTO input);
    Task<EmployeeTransferResultDTO> DecideAsync(int adminId, int requestId, string action, string? reason);
}
