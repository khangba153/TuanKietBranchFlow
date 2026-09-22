namespace TuanKietBranchFlow.Infrastructure.UnitOfWork;

public interface IUnitOfWork
{
    // Lưu toàn bộ thay đổi đang được DbContext theo dõi.
    Task<int> SaveChangesAsync();

    // Bắt đầu transaction cho 1 nghiệp vụ gồm nhiều thao tác db
    Task BeginTransactionAsync();

    // Xác nhận toàn bộ thay đổi trong transaction
    Task CommitTransactionAsync();

    // Hủy toàn bộ thay đổi nghiệp vụ gặp lỗi
    Task RollbackTransactionAsync();
}