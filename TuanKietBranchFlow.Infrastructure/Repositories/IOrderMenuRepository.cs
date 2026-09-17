using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public interface IOrderMenuRepository
{
    // Lấy danh mục, món và các size đang bán tại chi nhánh
    Task<List<Category>> GetAvailableCategoriesAsync(int branchId);

    // Lấy các nhóm topping đang hoạt động và được bán tại chi nhánh
    Task<List<ToppingGroup>> GetAvailableToppingGroupsAsync(int branchId);

    // Lấy các nhóm ghi chú và lựa chọn đang hoạt động
    Task<List<NoteGroup>> GetActiveNoteGroupsAsync();
}