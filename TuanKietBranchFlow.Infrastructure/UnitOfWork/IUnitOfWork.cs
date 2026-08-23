namespace TuanKietBranchFlow.Infrastructure.UnitOfWork;

public interface IUnitOfWork
{
    // Lưu toàn bộ thay đổi đang được DbContext theo dõi.
    Task<int> SaveChangesAsync();
}