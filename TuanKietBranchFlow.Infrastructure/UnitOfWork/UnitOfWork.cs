using Microsoft.EntityFrameworkCore.Storage;
using TuanKietBranchFlow.Infrastructure.Data;

namespace TuanKietBranchFlow.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly BranchFlowDbContext _context;
    private IDbContextTransaction? _currentTransaction;

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

    // Mở toàn bộ transaction để các repository dùng chung cùng DbContext
    public async Task BeginTransactionAsync()
    {
        if (_currentTransaction is not null)
        {
            throw new InvalidOperationException(
                "Một transaction đang được thực hiện.");
        }

        _currentTransaction =
            await _context.Database.BeginTransactionAsync();
    }

    // Commit transaction sau khi toàn bộ dữ liệu đã được lưu thành công
    public async Task CommitTransactionAsync()
    {
        if (_currentTransaction is null)
        {
            throw new InvalidOperationException(
                "Không có transaction để commit.");
        }

        IDbContextTransaction transaction = _currentTransaction;

        await transaction.CommitAsync();

        _currentTransaction = null;
        await transaction.DisposeAsync();
    }

    // Rollback transaction khi bất kỳ bước nào của nghiệp vụ gặp lỗi
    public async Task RollbackTransactionAsync()
    {
        if (_currentTransaction is null)
        {
            return;
        }

        IDbContextTransaction transaction = _currentTransaction;
        _currentTransaction = null;

        try
        {
            await transaction.RollbackAsync();
        }
        finally
        {
            await transaction.DisposeAsync();
        }
    }
}