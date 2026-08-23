using System.Linq.Expressions;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public interface IRepositoryBase<TEntity> where TEntity : class
{
    // Lấy toàn bộ dữ liệu của entity
    Task<List<TEntity>> GetAllAsync();

    // Tìm entity theo 1 hoặc nhiều giá trị khóa chính
    Task<TEntity?> GetByIdAsync(params object[] keyValues);

    // Lấy 1 entity duy nhất thỏa mãn điều kiện
    Task<TEntity?> SingleOrDefaultAsync(
        Expression<Func<TEntity, bool>> condition);
    
    // Lấy danh sách entity thỏa mãn điều kiện
    Task<List<TEntity>> WhereAsync(
        Expression<Func<TEntity, bool>> condition);
    
    // Đánh dấu 1 entity cần được thêm vào database.
    Task AddAsync(TEntity entity);

    // Đánh dấu 1 entity cần được cập nhật
    void Update(TEntity entity);
}