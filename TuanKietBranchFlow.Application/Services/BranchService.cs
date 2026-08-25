using TuanKietBranchFlow.Application.DTOs.Branches;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories;

namespace TuanKietBranchFlow.Application.Services;

public class BranchService : IBranchService
{
    private readonly IBranchRepository _branchRepository;

    // Nhận BranchRepository từ DI
    public BranchService(IBranchRepository branchRepository)
    {
        _branchRepository = branchRepository;
    }

    // Lấy danh sách chi nhánh theo role và phạm vi phân công
    public async Task<List<AccessibleBranchDTO>> GetAccessibleBranchesAsync(int userId, string role)
    {
        List<Branch> branches;
        
        // OWNER được xem tất cả các chi nhánh chưa bị xóa
        if (role == "OWNER")
        {
            branches = await _branchRepository.GetAllNotDeletedAsync();
        }
        else
        {
            // ADMIN và EMPLOYEE chỉ được xem chi nhánh được phân công
            DateOnly currentDate = DateOnly.FromDateTime(DateTime.Today);

            branches = await _branchRepository.GetAssignedBranchesAsync(userId, currentDate);
        }

        // Chuyển enity Branch thành DTO
        List<AccessibleBranchDTO> result = branches.Select(branch => new AccessibleBranchDTO
        {
            Id = branch.Id,
            Code = branch.Code,
            Name = branch.Name,
            Address = branch.Address,
            IsActive = branch.IsActive
        }).ToList();

        return result;
    }
}