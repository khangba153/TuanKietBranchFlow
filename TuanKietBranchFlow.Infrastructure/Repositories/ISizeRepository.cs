using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public interface ISizeRepository : IRepositoryBase<Size>
{
    // Lấy các size chưa bị xóa cho màn quản lý
    Task<List<Size>> GetAllNotDeletedAsync();

    // Kiểm tra tên đã được Size chưa xóa mềm sử dụng chưa
    Task<bool> ExistsByNameAsync(string name);

    // Lấy Size chưa xóa theo Id để cập nhật.
    Task<Size?> GetNotDeletedByIdAsync(int sizeId);

    // Kiểm tra tên đã được Size khác chưa xóa sử dụng chưa.
    Task<bool> ExistsByNameExceptIdAsync(string name, int sizeId);

    // Lấy các Size đang hoạt động, chưa xóa theo danh sách Id
    Task<List<Size>> GetActiveNotDeletedByIdsAsync(List<int> sizeIds);
}