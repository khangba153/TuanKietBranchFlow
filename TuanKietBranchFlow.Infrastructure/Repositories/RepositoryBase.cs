using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Infrastructure.Data;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public class RepositoryBase<TEntity> : IRepositoryBase<TEntity> where TEntity : class
{
    protected readonly BranchFlowDbContext Context;

    // Nhận DbContext từ Dependency Injection để truy cập database
    public RepositoryBase(BranchFlowDbContext context)
    {
        Context = context;
    }

    // Lấy toàn bộ dữ liệu của entity từ db
    public async Task<List<TEntity>> GetAllAsync()
    {
        return await Context.Set<TEntity>().ToListAsync();
    }

    // Tìm entity theo 1 hoặc nhiều giá trị khóa chính
    public async Task<TEntity?> GetByIdAsync(params object[] keyValues)
    {
        return await Context.Set<TEntity>().FindAsync(keyValues);
    }

    // Lấy 1 entity duy nhất thỏa mãn điều kiện
    public async Task<TEntity?> SingleOrDefaultAsync(
        Expression<Func<TEntity, bool>> condition)
    {
        return await Context.Set<TEntity>().SingleOrDefaultAsync(condition);
    }

    // Lấy danh sách entity thỏa mãn điều kiện
    public async Task<List<TEntity>> WhereAsync(
        Expression<Func<TEntity, bool>> condition)
    {
        return await Context.Set<TEntity>().Where(condition).ToListAsync();
    }

    // Đánh dấu entity cần được thêm vào db
    public async Task AddAsync(TEntity entity)
    {
        await Context.Set<TEntity>().AddAsync(entity);
    }

    // Đánh dấu entity cần được cập nhật
    public void Update(TEntity entity)
    {
        Context.Set<TEntity>().Update(entity);
    }


}