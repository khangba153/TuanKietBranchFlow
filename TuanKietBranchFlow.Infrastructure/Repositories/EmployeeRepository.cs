using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Infrastructure.Data;
using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public class EmployeeRepository : RepositoryBase<EmployeeProfile>, IEmployeeRepository
{
    // Nhận DbContext từ DI
    public EmployeeRepository(BranchFlowDbContext context) : base(context)
    {    
    }
    // Lấy nhân viên thuộc chi nhánh và áp dụng các điều kiện cần tìm kiếm
    public async Task<List<EmployeeProfile>> GetByBranchAsync(
        int branchId, 
        string keyword, 
        bool? isActive, 
        DateOnly currentDate)
    {
       // Tạo câu truy vấn và tải AppUser để lấy họ tên, trạng thái
       IQueryable<EmployeeProfile> query = Context.EmployeeProfiles
            .Include(employee => employee.User)
            .Where(employee => 
            !employee.Deleted
            && !employee.User.Deleted
            && employee.User.UserBranches.Any(userBranch =>
                userBranch.BranchId == branchId
                && userBranch.ActiveFrom <= currentDate
                && (userBranch.ActiveTo == null
                    || userBranch.ActiveTo >= currentDate)
                && !userBranch.Branch.Deleted));

        // Nếu có từ khóa thì tìm theo họ tên hoặc mã  nhân viên
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            string normalizedKeyword = keyword.Trim();

            query = query.Where(employee =>
                employee.User.FullName.Contains(normalizedKeyword)
                || employee.EmployeeCode.Contains(normalizedKeyword));
        }

        // Chỉ lọc trạng thái khi frontend có truyền isActive
        if (isActive.HasValue)
        {
            query = query.Where(employee =>
                employee.User.IsActive == isActive.Value);
        }

        // Câu SQL chỉ được thực thi khi gọi ToListAsync
        return await query.OrderBy(employee => employee.EmployeeCode).ToListAsync();
    }

    // Lấy chi tiết 1 nhân viên cùng thông tin User và Lịch sử chi nhánh
    public async Task<EmployeeProfile?> GetDetailByIdAndBranchAsync(
        int employeeId,
        int branchId,
        DateOnly currentDate)
    {
        return await Context.EmployeeProfiles
            .Include(employee => employee.User)
            .ThenInclude(user => user.UserBranches)
            .ThenInclude(userBranch => userBranch.Branch)
            .SingleOrDefaultAsync(employee =>
                employee.Id == employeeId
                && !employee.Deleted
                && !employee.User.Deleted
                && employee.User.UserBranches.Any(userBranch =>
                    userBranch.BranchId == branchId
                    && userBranch.ActiveFrom <= currentDate
                    && (userBranch.ActiveTo == null
                        || userBranch.ActiveTo >= currentDate)
                    && !userBranch.Branch.Deleted));
    }

    // Kiểm tra mã nhân viên đã tồn tại hay chưa
    public async Task<bool> EmployeeCodeExistsAsync(string employeeCode)
    {
        return await Context.EmployeeProfiles.AnyAsync(employee =>
            !employee.Deleted
            && employee.EmployeeCode == employeeCode);
    }
}