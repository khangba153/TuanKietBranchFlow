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
    // Lấy danh sách nhân viên theo chi nhánh, trạng thái và từ khóa tìm kiếm
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
             && !employee.User.Deleted);

        // Trường hợp lấy nhân viên đang làm việc tại chi nhánh
        if (isActive == true)
        {
            query = query.Where(employee =>
                employee.User.IsActive
                && employee.User.UserBranches.Any(userBranch =>
                    userBranch.BranchId == branchId
                    && !userBranch.Branch.Deleted
                    && userBranch.ActiveFrom <= currentDate
                    && (userBranch.ActiveTo == null
                        || userBranch.ActiveTo >= currentDate)));
        }

        // Trường hợp lấy nhân viên đã nghỉ việc tại chi nhánh
        else if (isActive == false)
        {
            query = query.Where(employee =>
                !employee.User.IsActive
                && employee.LeaveDate.HasValue
                && employee.LeaveDate.Value <= currentDate
                && employee.User.UserBranches.Any(userBranch =>
                    userBranch.BranchId == branchId
                    && !userBranch.Branch.Deleted
                    && userBranch.ActiveTo.HasValue
                    && userBranch.ActiveTo.Value == employee.LeaveDate.Value.AddDays(-1)));
        }

        // Không truyền trạng thái thì lấy cả nhân viên đang làm và đã nghỉ
        else
        {
            query = query.Where(employee =>
            (
                employee.User.IsActive
                && employee.User.UserBranches.Any(userBranch =>
                    userBranch.BranchId == branchId
                    && !userBranch.Branch.Deleted
                    && userBranch.ActiveFrom <= currentDate
                    && (userBranch.ActiveTo == null
                        || userBranch.ActiveTo >= currentDate))
            )
            ||
            (
                !employee.User.IsActive
                && employee.LeaveDate.HasValue
                && employee.LeaveDate.Value <= currentDate
                && employee.User.UserBranches.Any(userBranch =>
                    userBranch.BranchId == branchId
                    && !userBranch.Branch.Deleted
                    && userBranch.ActiveTo.HasValue
                    && userBranch.ActiveTo.Value == employee.LeaveDate.Value.AddDays(-1))
            ));
        }
        // Nếu có từ khóa thì tìm theo họ tên hoặc mã nhân viên
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            string normalizedKeyword = keyword.Trim();

            query = query.Where(employee =>
                employee.User.FullName.Contains(normalizedKeyword)
                || employee.EmployeeCode.Contains(normalizedKeyword));
        }

        // Sắp xep theo mã nhân viên rồi thực thi câu truy vấn
        return await query.OrderBy(employee => employee.EmployeeCode).ToListAsync();


    }

    // Lấy chi tiết 1 nhân viên cùng thông tin User và Lịch sử chi nhánh
    public async Task<EmployeeProfile?> GetDetailByIdAndBranchAsync(
        int employeeId,
        int branchId,
        DateOnly currentDate)
    {
        return await Context.EmployeeProfiles
            // Tải AppUser để lấy thông tin tài khoản nhân viên
            .Include(employee => employee.User)

            // Tải toàn bộ lịch sử phân công và thông tin chi nhánh
            .ThenInclude(user => user.UserBranches)
            .ThenInclude(userbranch => userbranch.Branch)

            // Tìm đúng 1 nhân viên hợp lệ tại chi nhánh
            .SingleOrDefaultAsync(employee =>
                employee.Id == employeeId
                && !employee.Deleted
                && !employee.User.Deleted
                && (
                    // TH1: nhân viên đang làm tại chi nhánh
                    (
                        employee.User.IsActive
                        && employee.User.UserBranches.Any(userBranch =>
                            userBranch.BranchId == branchId
                            && !userBranch.Branch.Deleted
                            && userBranch.ActiveFrom <= currentDate
                            && (userBranch.ActiveTo == null
                                || userBranch.ActiveTo >= currentDate))
                    )
                    ||
                    // TH2: nhân viên đã nghỉ tại chính chi nhánh này
                    (
                        !employee.User.IsActive
                        && employee.LeaveDate.HasValue
                        && employee.LeaveDate.Value <= currentDate
                        && employee.User.UserBranches.Any(userBranch =>
                            userBranch.BranchId == branchId
                            && !userBranch.Branch.Deleted
                            && userBranch.ActiveTo.HasValue
                            && userBranch.ActiveTo.Value == employee.LeaveDate.Value.AddDays(-1))
                    )
                ));
    }

    // Kiểm tra mã nhân viên đã tồn tại hay chưa
    public async Task<bool> EmployeeCodeExistsAsync(string employeeCode)
    {
        return await Context.EmployeeProfiles.AnyAsync(employee =>
            !employee.Deleted
            && employee.EmployeeCode == employeeCode);
    }

    // Kiểm tra mã nhân viên có được hồ sơ khác sử dụng hay không
    public async Task<bool> EmployeeCodeExistsForOtherEmployeeAsync(
        string employeeCode, int currentEmployeeId)
    {
        return await Context.EmployeeProfiles.AnyAsync(employee =>
            !employee.Deleted
            && employee.Id != currentEmployeeId
            && employee.EmployeeCode == employeeCode);
    }

    // Lấy nhân viên cùng AppUser và toàn bộ lịch sử phân công chi nhánh
    public async Task<EmployeeProfile?> GetByIdWithUserAndBranchesAsync(int employeeId)
    {
        return await Context.EmployeeProfiles
            .Include(employee => employee.User)
            .ThenInclude(user => user.UserBranches)
            .ThenInclude(userBranch => userBranch.Branch)
            .SingleOrDefaultAsync(employee =>
                employee.Id == employeeId
                && !employee.Deleted
                && !employee.User.Deleted);
    }
}