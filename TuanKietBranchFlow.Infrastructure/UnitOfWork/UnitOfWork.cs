using TuanKietBranchFlow.Infrastructure.Data;

namespace TuanKietBranchFlow.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly BranchFlowDbContext _context;

    // Nhận DbContext từ DI để quản lý việc lưu dữ liệu
    public UnitOfWork(BranchFlowDbContext context)
    {
        _context = context;
    }

    // Lưu toàn bộ thay đổi đang được các repository chuẩn bị
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
    
}