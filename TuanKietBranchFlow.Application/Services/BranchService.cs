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

    // Lấy 1 chi nhánh và kiểm tra phạm vi truy cập của người dùng
    public async Task<BranchAccessResultDTO> GetAccessibleBranchByIdAsync(int userId, string role, int branchId)
    {
        // Kiểm tra branch có tồn tại và bị xóa không
        Branch? branch = await _branchRepository.GetNotDeletedByIdAsync(branchId);

        if (branch == null)
        {
            return new BranchAccessResultDTO
            {
                IsFound = false,
                HasAccess = false,
                Branch = null
            };
        }

        // OWNER không cần kiểm tra bảng phân công UserBranch
        if (role == "OWNER")
        {
            return new BranchAccessResultDTO
            {
                IsFound = true,
                HasAccess = true,
                Branch = new AccessibleBranchDTO
                {
                    Id = branch.Id,
                    Code = branch.Code,
                    Name = branch.Name,
                    Address = branch.Address,
                    IsActive = branch.IsActive
                }
            };
        }

        // ADMIN và EMPLOYEE phải có phân công còn hiểu lực
        DateOnly currentDate = DateOnly.FromDateTime(DateTime.Today);

        bool hasAccess = await _branchRepository.HasActiveAssignmentAsync(userId, branchId, currentDate);

        if (!hasAccess)
        {
            return new BranchAccessResultDTO
            {
                IsFound = true,
                HasAccess = false,
                Branch = null
            };
        }

        // Trả thông tin Branch khi người dùng có quyền truy cập
        return new BranchAccessResultDTO
        {
            IsFound = true,
            HasAccess = true,
            Branch = new AccessibleBranchDTO
            {
                Id = branch.Id,
                Code = branch.Code,
                Name = branch.Name,
                Address = branch.Address,
                IsActive = branch.IsActive
            }
        };
    }
}